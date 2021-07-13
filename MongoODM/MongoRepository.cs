using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Core.Properties;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoODM.Abstractions;
using MongoODM.Attributes;
using MongoODM.Helpers;

namespace MongoODM
{
    /// <summary>
    /// The repository base class. Contains the population mechanism and shorthands/helpers for the Mongo API.
    /// </summary>
    /// <typeparam name="T">The entity type this class represents</typeparam>
    public class MongoRepository<T> : IMongoRepository<T> where T : Entity
    {
        private readonly IMongoDatabase _database;
        private readonly Dictionary<string, bool> _populates;
        private readonly List<CreateIndexModel<T>> _indices;
        
        /// <summary>
        /// The mongoCollection that is used in the repository
        /// </summary>
        public IMongoCollection<T> Collection { get; }

        /// <summary>
        /// Enables/disables logging to the console
        /// </summary>
        public bool UseLogging { get; set; }

        /// <summary>
        /// Indicates if you want to use an aggregate to $lookup some fields before population
        /// This reduces the amount of queries created during population
        /// The $lookup is less performant than just the DoPopulate that uses $in.
        /// ISSUE: $lookup does not work on single document
        /// </summary>
        public bool UseLookup { get; set; }

        /// <summary>
        /// Indicates if you want to allow MongoODM to populate user-defined fields, decorated by attributes like [Embed] or [Reference]
        /// </summary>
        public bool DoPopulate { get; set; } = true;
        
        /// <summary>
        /// Indicates if you want to automatically convert list of objects to list of references in the depopulate mechanism.
        /// Currently you must enable it manually to use this feature
        /// </summary>
        public bool UseDepopulate { get; set; }

        //Methods used for reflection part
        private readonly MethodInfo _castMethod;
        private readonly MethodInfo _toListMethod;
        private readonly MethodInfo _toArrayMethod;
        private readonly MethodInfo _deserializeMethod;

        /// <summary>
        /// Create new repository
        /// </summary>
        /// <param name="database">The database used</param>
        /// <param name="collection">The name of the collection, defaults to the name of the generic type in snake_case + s</param>
        public MongoRepository(IMongoDatabase database, string collection = null)
        {
            collection ??= typeof(T).Name.ToSnakeCase() + "s";
            _database = database;
            _populates = new Dictionary<string, bool>();
            _indices = new List<CreateIndexModel<T>>();
            
            Collection = _database.GetCollection<T>(collection);

            //Get reflection methods
            _castMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.Cast)) ??
                          throw new ArgumentException("Failed to get cast method");

            _toListMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.ToList)) ??
                            throw new ArgumentException("Failed to get toList method");

            _toArrayMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.ToArray)) ??
                             throw new ArgumentException("Failed to get toArray method");

            _deserializeMethod = typeof(BsonSerializer).GetMethods()
                .Where(x => x.Name == nameof(BsonSerializer.Deserialize))
                .First(x => x.IsGenericMethod);
        }

        /// <summary>
        /// Search type for index attributes
        /// </summary>
        /// <param name="t">The type to be searched</param>
        /// <param name="path">The path in the type</param>
        private void CreateIndexHook(Type t, string path = "")
        {
            Log("[Index] Checking " + t.Name + " at "+path);
            
            //Foreach properties
            foreach (var prop in t.GetProperties())
            {
                //Check attributes
                foreach (var attr in prop.GetCustomAttributes(true))
                {
                    //ReferenceAttribute: do nothing: OK
                    //EmbedAttribute: Run hook recursive: OK
                    //Other property, NOT a collection but actually an object: run hook? TBD

                    if (attr is EmbedAttribute)
                    {
                        Log("[Index-I] Recursive on "+prop.Name);
                        CreateIndexHook(prop.PropertyType, path + prop.Name+".");
                    }
                    else if (attr is IndexAttribute index)
                    {
                        var indexes = index.Indexes.Select(x => new Index {Field = path + x.Field, Type = x.Type}).ToList();
                        var indices = new List<IndexKeysDefinition<T>>();

                        foreach (var x in indexes)
                        {
                            switch (x.Type)
                            {
                                case IndexType.Ascending:
                                    indices.Add(Builders<T>.IndexKeys.Ascending(x.Field));
                                    break;
                                case IndexType.Descending:
                                    indices.Add(Builders<T>.IndexKeys.Descending(x.Field));
                                    break;
                                case IndexType.Hashed:
                                    indices.Add(Builders<T>.IndexKeys.Hashed(x.Field));
                                    break;
                                case IndexType.Text:
                                    indices.Add(Builders<T>.IndexKeys.Text(x.Field));
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }

                        var idx = indices.Count == 1 ? indices.First() : Builders<T>.IndexKeys.Combine(indices);
                        _indices.Add(new CreateIndexModel<T>(idx, new CreateIndexOptions {Unique = index.Unique, Background = true}));
                    }
                }
            }
        }

        /// <summary>
        /// Method to create indexes automatically
        /// </summary>
        public async Task CreateIndexesAsync() //TODO: rename
        {
            CreateIndexHook(typeof(T));
            await Collection.Indexes.CreateManyAsync(_indices);
            _indices.Clear();
        }

        /// <summary>
        /// Logger helper Logs message to console in debug, when logging is enabled
        /// </summary>
        /// <param name="message">The object to be logged</param>
        private void Log(object message)
        {
            if(UseLogging)
                Console.WriteLine(message);
        }

        /// <summary>
        /// The population logic. Is called recursively over all properties and is used to lookup and fill enabled references
        /// </summary>
        /// <param name="t">The type to be evaluated</param>
        /// <param name="value">The instance to be filled</param>
        /// <param name="path">The current path. Please do not provide</param>
        private void PopulateHook(Type t, object value, string path = "")
        {
            Log("![T]! Checking " + t.Name + " at "+path);
            foreach (var prop in t.GetProperties())
            {
                bool skip = false;
                bool index = false;

                Log("[P] Property " + prop.Name);
                
                var val = prop.GetValue(value);
                var propType = prop.PropertyType;
                
                //Check attributes
                foreach (var attr in prop.GetCustomAttributes(true))
                {
                    //If embed or reference attribute, make sure to index 
                    if (attr is IMongoAttribute)
                        index = true;

                    //If reference attribute, check if it is enabled
                    if (attr is ReferenceAttribute refAttr)
                    {
                        //Check if runtime populates must set the enabled property or not
                        if (_populates.TryGetValue(path + prop.Name, out bool isEnabled))
                        {
                            Log("[CREF] Found override populate on "+prop.Name);
                            refAttr.Enabled = isEnabled;
                        }
                        
                        //If not enabled, skip whole property (ignore everything)
                        if (!refAttr.Enabled)
                        {
                            Log("[XREF] Found disabled referenceAttribute, skipping");
                            skip = true;
                            break;
                        }

                        //If the property is already filled, we assume it is already populated
                        //But we still need to check everything in it, so skip is not set
                        if (val != null)
                        {
                            Log("[N-REF] Property is already filled!");
                            break;
                        }

                        Log("[REF] Found enabled referenceAttribute on " + prop.Name);

                        //Get values
                        var refFieldValue = Tools.GetValue(refAttr.LocalField, value);
                        
                        //If no references are present, ignore and skip
                        if (refFieldValue == null)
                        {
                            Log("[XREF] No references are present");
                            skip = true;
                            break;
                        }

                        //Convert ref(s) to ICollection of objectIds
                        var isRefCollection = refFieldValue.GetType().IsCollection(out Type refFieldType);
                        ICollection colRefs = isRefCollection
                            ? (ICollection)refFieldValue
                            : new List<object> {refFieldValue};

                        var listRefs = new List<object>();
                        var listEnum = colRefs.GetEnumerator();
                        while (listEnum.MoveNext())
                            listRefs.Add(listEnum.Current);
                        
                        ICollection<ObjectId> refs;
                        
                        switch (refFieldType.Name)
                        {
                            case "String":
                                refs = listRefs.Select(x => ObjectId.Parse((string) x)).ToList();
                                break;
                            case "ObjectId":
                                refs = listRefs.Select(x => (ObjectId)x).ToList();
                                break;
                            default:
                                throw new ArgumentException("Only strings and ObjectId are accepted as key");
                        }

                        //Lookup document
                        var refCollection = _database.GetCollection<BsonDocument>(refAttr.RefCollection);
                        
                        var filter = Builders<BsonDocument>.Filter.In(refAttr.RefField, refs);
                        var query = refCollection.Find(filter).ToList();

                        //Get output type
                        var outputType = propType;
                        var isArray = outputType.IsArray;
                        var isCollection = outputType.IsCollection(out outputType);

                        //Deserialize results
                        var deserialize = _deserializeMethod.MakeGenericMethod(outputType);
                        var queryResults = query.Select(x =>
                            deserialize.Invoke(null, new object[] {x, null})).ToList();
                        
                        //Convert results to desired type
                        var cast = _castMethod.MakeGenericMethod(outputType);
                        var castResults = cast.Invoke(null, new object[] {queryResults});

                        var toList = _toListMethod.MakeGenericMethod(outputType);
                        var results = toList.Invoke(null, new[] {castResults});

                        //Check if the output type is an collection. If so, fill with array or list value
                        if (isCollection)
                        {
                            Log("Set value to " + prop.Name + ", is " + results);
                            if (isArray)
                            {
                                var toArray = _toArrayMethod.MakeGenericMethod(outputType);
                                prop.SetValue(value, toArray.Invoke(null, new[] {results}));
                            }
                            else
                            {
                                //Set list value
                                prop.SetValue(value, results);
                            }
                        }
                        else
                        {
                            //If not, get first item (always 1, we assume) and fill it
                            Log("Set value to " + prop.Name + ", is " + results);
                            var res = ((IList) results)?[0];
                            prop.SetValue(value, res);
                        }

                        //Make sure the val variable now is set correctly
                        //val = value might work, but this is more consistent???
                        val = prop.GetValue(value);
                    }
                }

                Log("Property " + prop.Name + " with value " + (val ?? "NULL"));

                //If skip is triggered, value is still null or value is not needed to be indexed skip property
                if (skip || val == null || !index)
                {
                    Log("[SKIP] Skipping " + prop.Name);
                    continue;
                }

                //Check if property is collection. If so, run populate hook over every item in the collection
                if (propType.IsCollection(out t))
                {
                    Log("Collection containing " + t.Name);
                    var cast = _castMethod.MakeGenericMethod(t);
                    var casted = cast.Invoke(null, new[] {val});
                    if (casted != null)
                    {
                        foreach (var c in (IEnumerable) casted)
                            PopulateHook(t, c, path+prop.Name+".");
                    }
                    else
                    {
                        Log("[E] CASTED IS NULL!");
                    }
                }
                else //If not, run populate hook over object
                {
                    Log("Type is Entity, index");
                    PopulateHook(t, val, path+prop.Name+".");
                }
            }
        }

        /// <summary>
        /// Depopulation hook, used to remove the populated values from an object
        /// That means, ALL populated values are set to NULL
        /// </summary>
        /// <param name="t">The type to be evaluated</param>
        /// <param name="value">The value to be set</param>
        private void DepopulateHook(Type t, object value)
        {
            foreach (var prop in t.GetProperties())
            {
                Log("[DP] Checking "+prop.Name);
                foreach (var attr in prop.GetCustomAttributes())
                {
                    //If embed attribute, check if collection
                    if (attr is EmbedAttribute)
                    {
                        var val = prop.GetValue(value);
                        if (val == null)
                        {
                            Log("[DPE] value is null");
                            continue;
                        }
                        
                        //If collection, foreach it into the hook recursively
                        if (prop.PropertyType.IsCollection(out t))
                        {
                            var cast = _castMethod.MakeGenericMethod(t);
                            var casted = cast.Invoke(null, new[] {val});
                            if (casted != null)
                            {
                                foreach (var c in (IEnumerable) casted)
                                    DepopulateHook(t, c);
                            }
                        }
                        else //If not, run the populate hook on the single property
                        {
                            DepopulateHook(t, val);
                        }
                    }
                    
                    //If reference attribute, set its value to null and do not run any recursion
                    if (attr is ReferenceAttribute refAttr)
                    {
                        //TODO: depopulate hook now only works with strings
                        if (UseDepopulate)
                        {
                            var refProp = t.GetProperty(refAttr.LocalField) ?? throw new ArgumentException("Failed to get property");
                            var fieldValue = prop.GetValue(value) ?? throw new ArgumentException("Failed to get fieldValue");
                        
                            if (fieldValue.GetType().IsCollectionOf(typeof(Entity), out _))
                                refProp.SetValue(value, ((ICollection<Entity>)fieldValue).Select(x => x.Id).ToList());
                            else
                                refProp.SetValue(value, ((Entity)fieldValue).Id);
                        }
                        
                        prop.SetValue(value, null);
                    }
                }
            }
        }

        /// <summary>
        /// Shorthand for the population hook
        /// </summary>
        /// <param name="value">The value to be populated</param>
        protected void PopulateHook(T value)
        {
            if (!DoPopulate)
                return;
            
            PopulateHook(typeof(T), value);
        }

        /// <summary>
        /// Shorthand for the depopulation hook.
        /// </summary>
        /// <param name="value">The value to be depopulated</param>
        /// <returns>Depopulated value</returns>
        protected T DepopulateHook(T value)
        {
            if (!DoPopulate)
                return value;
            
            var val = value.Clone();
            DepopulateHook(typeof(T), val);
            return (T)val;
        }
        
        /// <summary>
        /// Creates an aggregate hook to lookup some fields before population
        /// </summary>
        /// <param name="aggregate">The aggregate to add the stages to</param>
        /// <param name="t">The type to reference</param>
        /// <param name="settings">The settings. Please leave empty, used in recursion</param>
        private void BuildAggregateHook(ref IAggregateFluent<BsonDocument> aggregate, Type t, AggregateSettings settings = null)
        {
            settings ??= new AggregateSettings();

            foreach (var prop in t.GetProperties())
            {
                Log("[AGG] Property "+ settings.GetPath(prop.Name));
                foreach (var attr in prop.GetCustomAttributes())
                {
                    //If reference attribute, add a lookup for it
                    if (attr is ReferenceAttribute refAttr)
                    {
                        if (_populates.TryGetValue(settings.GetPath(prop.Name), out var isEnabled))
                            refAttr.Enabled = isEnabled;
                        
                        if(!refAttr.Enabled)
                            break;
                        
                        Log("[AggR] Enabled reference found");

                        var localField = settings.GetPath(refAttr.LocalField);
                        var targetField = settings.GetPath(prop.Name);

                        aggregate = aggregate.Lookup(refAttr.RefCollection, localField, refAttr.RefField, targetField);
                        if (!prop.PropertyType.IsCollection(out _))
                            aggregate = aggregate.Unwind(targetField);
                    }

                    //If embed, create aggregate for it
                    if (attr is EmbedAttribute)
                    {
                        if(!settings.DoNest)
                            return;
                        
                        bool isCollection = prop.PropertyType.IsCollection(out Type elementType);

                        //If collection, add $unwind
                        if (isCollection)
                            aggregate = aggregate.Unwind(settings.GetPath(prop.Name));

                        //Call build aggregate hook
                        BuildAggregateHook(ref aggregate, elementType, new AggregateSettings{Path = settings.GetPath(prop.Name)+".", DoNest = false, ParentType = t, IsCollection = isCollection});
                        AddGroupHook(settings.GetPath(prop.Name), t, ref aggregate);
                    }
                }
            }
        }

        /// <summary>
        /// Add an automated $group stage to an existing aggregate
        /// </summary>
        /// <param name="target">The target field</param>
        /// <param name="parentType">The type of the parent (containing properties)</param>
        /// <param name="aggregate">The aggregate to be modified</param>
        private void AddGroupHook(string target, Type parentType, ref IAggregateFluent<BsonDocument> aggregate)
        {
            var doc = new BsonDocument("_id", "$_id");
            
            //Get parent properties, not id or name of target collection
            var props = parentType.GetProperties().Where(x => x.Name != "Id" && x.Name != target)
                .Select(x => (x.Name, new BsonDocument("$first", "$" + x.Name)))
                .ToDictionary(x => x.Item1, y => y.Item2);
            doc.AddRange(props);
            doc.Add(target, new BsonDocument("$push", "$" + target));
            
            aggregate = aggregate.Group(doc);
        }
        
        /// <summary>
        /// Create an aggregate using the BuildAggregateHook to $lookup some fields before population
        /// </summary>
        /// <returns>IAggregateFluent of type T</returns>
        private IAggregateFluent<T> CreateAggregate()
        {
            var aggregate = Collection.Aggregate().As<BsonDocument>();
            
            if(UseLookup)
                BuildAggregateHook(ref aggregate, typeof(T));
            
            return aggregate.As<T>();
        }

        /// <summary>
        /// Sets a population change in the internal dictionary
        /// Used when populating, checks whether the attribute must be used
        /// </summary>
        /// <param name="paths">The path to be set, like User.Emails</param>
        /// <param name="enabled">True to enable, false to disable</param>
        private void SetPopulate(bool enabled, params string[] paths)
        {
            foreach (var path in paths)
            {
                if (!_populates.TryAdd(path, enabled))
                    _populates[path] = enabled;
            }
        }

        /// <summary>
        /// Override a specific reference attribute to be enabled in population
        /// </summary>
        /// <param name="paths">The object(s) to be populated</param>
        public void Populate(params string[] paths) => SetPopulate(true, paths);
        
        /// <summary>
        /// Override a specific reference attribute to be disabled in population
        /// </summary>
        /// <param name="paths">The object to be disabled</param>
        public void Depopulate(params string[] paths) => SetPopulate(false, paths);

        /// <summary>
        /// Reset all populate attribute overrides
        /// </summary>
        public void ResetPopulates() => _populates.Clear();

        /// <summary>
        /// Creates a filter for one item's ID
        /// </summary>
        /// <param name="id">The id to be filtered</param>
        /// <returns>Filter</returns>
        public FilterDefinition<T> GetFilter(string id)
        {
            //TODO: Decide if validation happens here
            //if (!Constants.ObjectIdRegex.IsMatch(id))
            //    throw new ArgumentException("Id is not a valid objectId string");
            
            return Builders<T>.Filter.Eq(x => x.Id, id);
        }

        /*
         * CRUD methods
         */

        /// <summary>
        /// Adds a document to the collection
        /// </summary>
        /// <param name="document">The document to be added</param>
        public void Insert(T document)
        {
            var doc = DepopulateHook(document);
            Collection.InsertOne(doc);
            document.Id = doc.Id;
        }

        /// <summary>
        /// Add multiple documents to the collection
        /// </summary>
        /// <param name="documents">The documents to be added</param>
        public void InsertMany(params T[] documents)
        {
            Collection.InsertMany(documents.Select(DepopulateHook));
        }

        /// <summary>
        /// Get first item from collection matching a filter
        /// </summary>
        /// <param name="filter">The filter to apply</param>
        /// <returns>The first element matching the filter or NULL</returns>
        public T First(FilterDefinition<T> filter = null)
        {
            filter ??= FilterDefinition<T>.Empty;
            var first = CreateAggregate().Match(filter).FirstOrDefault();
            if (first == null)
                return null;
            
            PopulateHook(first);
            return first;
        }

        /// <summary>
        /// Get all elements in collection matching a filter
        /// </summary>
        /// <param name="filter">The filter to check</param>
        /// <returns>All elements matching filter</returns>
        public IEnumerable<T> Find(FilterDefinition<T> filter = null)
        {
            filter ??= FilterDefinition<T>.Empty;
            var cur = CreateAggregate().Match(filter).ToCursor();
            foreach (var e in cur.ToEnumerable())
            {
                PopulateHook(e);
                yield return e;
            }
        }

        /// <summary>
        /// Count all documents in collection matching optional filter
        /// </summary>
        /// <param name="filter">Optional: filter</param>
        /// <returns>The amount of documents matching a filter</returns>
        public long Count(FilterDefinition<T> filter = null)
        {
            filter ??= FilterDefinition<T>.Empty;
            return Collection.CountDocuments(filter);
        }

        /// <summary>
        /// Update document
        /// </summary>
        /// <param name="document">The document to be updated</param>
        /// <returns>The updated document</returns>
        public T Update(T document)
        {
            var doc = DepopulateHook(document);
            return Collection.FindOneAndReplace(GetFilter(doc.Id), doc);
        }

        /// <summary>
        /// Update one document matching filter
        /// </summary>
        /// <param name="filter">The filter</param>
        /// <param name="update">The update definition</param>
        /// <returns>True if update succeeds</returns>
        public bool Update(FilterDefinition<T> filter, UpdateDefinition<T> update)
            => Collection.UpdateOne(filter, update).IsAcknowledged;

        /// <summary>
        /// Update many documents using filter and updateDefinition
        /// </summary>
        /// <param name="filter">The filter to apply</param>
        /// <param name="update">The update</param>
        /// <returns>UpdateResult</returns>
        public UpdateResult UpdateMany(FilterDefinition<T> filter, UpdateDefinition<T> update)
            => Collection.UpdateMany(filter, update);

        /// <summary>
        /// Delete an object by its id
        /// </summary>
        /// <param name="id">The id of the object to be deleted</param>
        /// <returns>True if delete is acknowledged</returns>
        public bool Delete(string id)
        {
            var res = Collection.DeleteOne(GetFilter(id));
            return res.IsAcknowledged;
        }

        /// <summary>
        /// Delete all documents matching a filter
        /// </summary>
        /// <param name="filter">The filter to look for</param>
        /// <returns>The amount of deleted documents. -1 if failed</returns>
        public int DeleteMany(FilterDefinition<T> filter)
        {
            var res = Collection.DeleteMany(filter);
            return res.IsAcknowledged ? (int)res.DeletedCount : -1;
        }
    }
}