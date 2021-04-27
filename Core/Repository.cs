using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Core.Abstractions;
using Core.Attributes;
using Core.Helpers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Core
{
    public class Repository<T> where T : Entity
    {
        private readonly IMongoDatabase _database;
        public IMongoCollection<T> Collection { get; }
        private readonly Dictionary<string, bool> _populates;
        
        //Methods used for reflection part
        private readonly MethodInfo _castMethod;
        private readonly MethodInfo _toListMethod;
        private readonly MethodInfo _toArrayMethod;
        private readonly MethodInfo _deserializeMethod;

        /// <summary>
        /// Create new repository
        /// </summary>
        /// <param name="database">The database used</param>
        /// <param name="collection">The name of the collection, defaults to the name of the generic type in snake_case</param>
        /// <exception cref="ArgumentException"></exception>
        public Repository(IMongoDatabase database, string collection = null)
        {
            collection ??= typeof(T).Name.ToSnakeCase();
            _database = database;
            Collection = _database.GetCollection<T>(collection);
            _populates = new Dictionary<string, bool>();

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
        /// The population logic. Is called recursively over all properties and is used to lookup and fill enabled references
        /// </summary>
        /// <param name="t">The type to be evaluated</param>
        /// <param name="value">The instance to be filled</param>
        /// <param name="path">The current path. Please do not provide</param>
        private void PopulateHook(Type t, object value, string path = "")
        {
            Console.WriteLine("![T]! Checking " + t.Name + " at "+path);
            foreach (var prop in t.GetProperties())
            {
                bool skip = false;
                bool index = false;

                Console.WriteLine("[P] Property " + prop.Name);
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
                        //Check if runtime populates must set the enabled propery or not
                        if (_populates.TryGetValue(path + prop.Name, out bool isEnabled))
                        {
                            Console.WriteLine("[CREF] Found override populate on "+prop.Name);
                            refAttr.Enabled = isEnabled;
                        }
                        
                        //If not enabled, skip whole propery (ignore everything)
                        if (!refAttr.Enabled)
                        {
                            Console.WriteLine("[XREF] Found disabled referenceAttribute, skipping");
                            skip = true;
                            break;
                        }

                        //If the propery is already filled, we assume it is already populated
                        //But we still need to check everything in it, so skip is not set
                        if (val != null)
                        {
                            Console.WriteLine("[NREF] Property is already filled!");
                            break;
                        }

                        Console.WriteLine("[REF] Found enabled referenceAttribute on " + prop.Name);

                        //Get values
                        var refFieldValue = Tools.GetValue(refAttr.LocalField, value);
                        
                        //If no references are present, ignore and skip
                        if (refFieldValue == null)
                        {
                            Console.WriteLine("[XREF] No references are present");
                            skip = true;
                            break;
                        }

                        //Convert ref(s) to ICollection of objectIds
                        var refs = !refFieldValue.GetType().IsCollection(out _)
                            ? new List<ObjectId> {(ObjectId) refFieldValue}
                            : (ICollection<ObjectId>) refFieldValue;

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
                        var castResults = cast.Invoke(null, new[] {queryResults});

                        var toList = _toListMethod.MakeGenericMethod(outputType);
                        var results = toList.Invoke(null, new[] {castResults});

                        //Check if the output type is an collection. If so, fill with array or list value
                        if (isCollection)
                        {
                            Console.WriteLine("Set value to " + prop.Name + ", is " + results);
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
                            Console.WriteLine("Set value to " + prop.Name + ", is " + results);
                            var res = ((IList) results)?[0];
                            prop.SetValue(value, res);
                        }

                        //Make sure the val variable now is set correctly
                        //val = value might work, but this is more consistent???
                        val = prop.GetValue(value);
                    }
                }

                Console.WriteLine("Property " + prop.Name + " with value " + (val ?? "NULL"));

                //If skip is triggered, value is still null or value is not needed to be indexed skip property
                if (skip || val == null || !index)
                {
                    Console.WriteLine("[SKIP] Skipping " + prop.Name);
                    continue;
                }

                //Check if property is collection. If so, run populate hook over every item in the collection
                if (propType.IsCollection(out t))
                {
                    Console.WriteLine("Collection containing " + t.Name);
                    var cast = _castMethod.MakeGenericMethod(t);
                    var casted = cast.Invoke(null, new[] {val});
                    if (casted != null)
                    {
                        foreach (var c in (IEnumerable) casted)
                            PopulateHook(t, c, path+prop.Name+".");
                    }
                    else
                    {
                        Console.WriteLine("[E] CASTED IS NULL!");
                    }
                }
                else //If not, run populate hook over object
                {
                    Console.WriteLine("Type is Entity, index");
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
                Console.WriteLine("[P] Checking "+prop.Name);
                foreach (var attr in prop.GetCustomAttributes())
                {
                    //If embed attribute, check if collection
                    if (attr is EmbedAttribute)
                    {
                        //If collection, foreach it into the hook recursively
                        if (prop.PropertyType.IsCollection(out t))
                        {
                            var cast = _castMethod.MakeGenericMethod(t);
                            var casted = cast.Invoke(null, new[] {prop.GetValue(value)});
                            if (casted != null)
                            {
                                foreach (var c in (IEnumerable) casted)
                                    DepopulateHook(t, c);
                            }
                        }
                        else //If not, run the populate hook on the single property
                        {
                            DepopulateHook(t, prop.GetValue(value));
                        }
                    }
                    
                    //If reference attribute, set its value to null and do not run any recursion
                    if (attr is ReferenceAttribute)
                    {
                        prop.SetValue(value, null);
                    }
                }
            }
        }

        /// <summary>
        /// Shorthand for the population hook
        /// </summary>
        /// <param name="value">The value to be populated</param>
        private void PopulateHook(T value)
            => PopulateHook(typeof(T), value);

        /// <summary>
        /// Shorthand for the depopulation hook.
        /// </summary>
        /// <param name="value">The value to be depopulated</param>
        /// <returns>Depopulated value</returns>
        private T DepopulateHook(T value)
        {
            var val = value.Clone();
            DepopulateHook(typeof(T), val);
            return (T)val;
        }
        
        /*
         * IN PROGRESS
         */

        
        // !! IN PROGRESS !!: Integrate in PopulateHook when done could increase performance
        private IAggregateFluent<BsonDocument> BuildAggregateHook(IAggregateFluent<BsonDocument> aggregate, Type t, string path = "")
        {
            foreach (var prop in t.GetProperties())
            {
                Console.WriteLine("[AGG] Property "+prop.Name);
                foreach (var attr in prop.GetCustomAttributes())
                {
                    //If reference attribute, add a lookup for it
                    if (attr is ReferenceAttribute refAttr)
                    {
                        if (_populates.TryGetValue(path + prop.Name, out bool isEnabled))
                            refAttr.Enabled = isEnabled;
                        
                        if(!refAttr.Enabled)
                            break;

                        var localField = path + refAttr.LocalField;
                        var targetField = path + prop.Name;

                        aggregate = aggregate.Lookup(refAttr.RefCollection, localField, refAttr.RefField, targetField);
                        if (!prop.PropertyType.IsCollection(out _))
                            aggregate = aggregate.Unwind(targetField);
                    }

                    /*if (attr is EmbedAttribute)
                    {
                        bool isCollection = prop.PropertyType.IsCollection(out Type elementType);

                        if (isCollection)
                            aggregate = aggregate.Unwind(prop.Name);

                        aggregate = BuildAggregateHook(aggregate, elementType, path + prop.PropertyType.Name + ".");
                        
                        //Build group
                        //Need other fields from aggregate hook for this
                    }*/
                }
            }

            return aggregate;
        }

        /// <summary>
        /// Creates a filter for one item's ID
        /// </summary>
        /// <param name="id">The id to be filtered</param>
        /// <returns>Filter</returns>
        private FilterDefinition<T> GetFilter(ObjectId id)
            => Builders<T>.Filter.Eq(x => x.Id, id);

        /// <summary>
        /// Sets a population change in the internal dictionary
        /// Used when populating, checks whether the attribute must be used
        /// </summary>
        /// <param name="path">The path to be set, like User.Emails</param>
        /// <param name="enabled">True to enable, false to disable</param>
        private void SetPopulate(string path, bool enabled)
        {
            if (!_populates.TryAdd(path, enabled))
                _populates[path] = enabled;
        }

        /// <summary>
        /// Override a specific reference attribute to be enabled in population
        /// </summary>
        /// <param name="path">The object to be populated</param>
        public void Populate(string path) => SetPopulate(path, true);
        
        /// <summary>
        /// Override a specific reference attribute to be disabled in population
        /// </summary>
        /// <param name="path">The object to be disabled</param>
        public void Depopulate(string path) => SetPopulate(path, false);

        /// <summary>
        /// Reset all populate attribute overrides
        /// </summary>
        public void ResetPopulates()
            => _populates.Clear();

        //How-to with async and await? Can i run async from sync method?
        //https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/async/

        //Methods, methods, methods

        //In progress method
        public T FirstBeta()
        {
            var aggregate = BuildAggregateHook(Collection.Aggregate().As<BsonDocument>(), typeof(T));
            var first = aggregate.Limit(1).As<T>().First();
            PopulateHook(first);
            return first;
        }

        public T First()
        {
            return Collection.Aggregate().Limit(1).First();
        }
        
        /*
         * CRUD methods
         *
         * TODO: make wrapper, like DbSet<>
         */
    }
}