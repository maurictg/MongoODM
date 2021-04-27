using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Core.Abstractions;
using Core.Actions;
using Core.Attributes;
using Core.Helpers;
using Core.Structures;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Core
{
    /// <summary>
    /// MongoDb ODM wrapper
    /// </summary>
    /// <typeparam name="T">The entity</typeparam>
    public class OldRepository<T> where T : Entity
    {
        private readonly IMongoCollection<T> _collection;
        private readonly Node<MapperAction> _actionTree;
        private readonly IMongoDatabase _database;
        
        /// <summary>
        /// The raw mongo collection
        /// </summary>
        public IMongoCollection<T> Collection => _collection;

        /// <summary>
        /// Create a new repository (or inherit from this class)
        /// </summary>
        /// <param name="name">The collection to be used by the repository</param>
        /// <param name="db">The mongo database containing the collection</param>
        public OldRepository(string name, IMongoDatabase db)
        {
            _database = db;
            _collection = _database.GetCollection<T>(name);
            //_actionTree = new Node<MapperAction>(null);
            //MapPopulate(typeof(T), _actionTree);
        }

        #if DEBUG
        public void debug()
        {
            var user = Collection.Find(x => true).First();
            MapPopulate(typeof(T), user);
            
            object at = user;
            
            //0. Hack method of BSON deserializer
            var deserializeMethod = typeof(BsonSerializer).GetMethods()
                .Where(x => x.Name == "Deserialize")
                .FirstOrDefault(x => x.IsGenericMethod);

            if (deserializeMethod == null)
                throw new ArgumentException(nameof(deserializeMethod));
            
            _actionTree.Visit(x =>
            {
                Console.WriteLine(x?.ToString() ?? "NULL");
                if (x is Lookup lo)
                {
                    //1. Get collection needed
                    var collection = _database.GetCollection<BsonDocument>(lo.RefCollection);

                    ICollection<ObjectId> refs;
                    
                    //2. Get reference(s)
                    var refFieldValue = ToolsOld.GetValue(lo.LocalField.LastPart(), at);
                    
                    if (!x.IsCollection)
                        refs = new List<ObjectId>{(ObjectId)refFieldValue};
                    else
                        refs = (ICollection<ObjectId>)refFieldValue;
                    
                    //3. Query other collection
                    var filter = Builders<BsonDocument>.Filter.In("_id", refs);
                    var query = collection.Find(filter).ToList();
                    
                    //4. Get output type
                    var outputType = ToolsOld.GetType(lo.Path.LastPart(), typeof(T));
                    var isCollection = outputType.IsCollection(out outputType);
                    
                    //5. Convert results to desired type
                    var deserialize = deserializeMethod.MakeGenericMethod(outputType);
                    var results = query.Select(x =>
                    {
                        return Convert.ChangeType(deserialize.Invoke(null, new object[] {x, null}), outputType);
                    }).ToList();
                    
                    //6. Set result in object
                    var result = isCollection ? ToolsOld.ConvertList(results, outputType) : Convert.ChangeType(results.First(), outputType);
                    //ToolsOld.SetValues(lo.Path.LastPart(), user, result);
                }
            });


        }

        #endif

        /// <summary>
        /// Find and map all populate attributes, also nested
        /// </summary>
        /// <param name="item">The item to be searched</param>
        /// <param name="path">Path. Please leave empty, used in recursion</param>
        private void MapPopulate(Type item, object value, string path = "")
        {
            var level = path.Count(x => x == '.');
            
            Console.WriteLine("Map populate at "+level+" for "+path);
            
            foreach (var p in item.GetProperties())
            {
                //Werkt natuurlijk niet voor collecties
                Console.WriteLine("Current type: "+value.GetType().Name);
                Console.WriteLine("Current: "+JsonSerializer.Serialize(value));
                Console.WriteLine("Looking at "+p.Name);
                
                var t = p.PropertyType;
                var val = p.GetValue(value);
                Console.WriteLine("Value is: "+val);

                //Get IMongoAttributes (Embed, Reference)
                foreach (var attr in p.GetCustomAttributes(true).Where(x => x is IMongoAttribute))
                {
                    //If reference attribute
                    if (attr is ReferenceAttribute refAttr)
                    {
                        var lookup = new Lookup
                        {
                            Enabled = refAttr.AutoPopulate,
                            LocalField = path + refAttr.LocalField,
                            Path = path + p.Name,
                            RefCollection = refAttr.RefCollection,
                            RefField = refAttr.RefField
                        };
                        
                        if (t.IsCollection(out t)) //lookup collection type and set t
                            lookup.IsCollection = true;
                        
                        if (level == 0)
                        {
                            //node.Value = lookup;
                        }
                        else
                        {
                            //node = node.Add(lookup);
                        }
                        
                        MapPopulate(t, val, path+p.Name+'.');
                        break;
                    }

                    //If embed attribute, just call recursion on it (it acts like a normal attribute)
                    if (attr is EmbedAttribute)
                    {
                        t.IsCollection(out t);
                        MapPopulate(t, val, path+p.Name+'.');
                        break;
                    }
                }
            }
        }
        
        /*
         *  Aggregate:
         *  - Check lookups and execute them. If no collection, add unwind
         *  
         */
        
        /*
        /// <summary>
        /// Toggles populate attribute
        /// </summary>
        /// <param name="field">The field to be toggled</param>
        /// <param name="on">On or off</param>
        private void ChangePopulate(string field, bool on)
        {
            var p = _populates.FirstOrDefault(x => x.TargetField == field);
            if (p == null)
                throw new ArgumentException("Field does not exist or not contain populate attribute");
            p.On = on;
        }*/
        

        /*
        /// <summary>
        /// Hook to run before update or insert. Empties target field for populates and fills the reference (localField) with references
        /// </summary>
        /// <param name="itemToDepopulate">The item to be cloned and modified</param>
        /// <returns>Modified item</returns>
        private T DepopulateHook(T itemToDepopulate)
        {
            var item = itemToDepopulate.Clone();
            foreach (var p in _populates.OrderBy(x => x.TargetField).Where(x => x.On))
                ToolsOld.SetValues(p.TargetField, item, null);

            return item as T;
        }*/

        /// <summary>
        /// Creates a filter for one item's ID
        /// </summary>
        /// <param name="id">The id to be filtered</param>
        /// <returns>Filter</returns>
        private FilterDefinition<T> GetFilter(ObjectId id)
            => Builders<T>.Filter.Eq(x => x.Id, id);
        
        /*
         * ODM (Object Document Mapper) methods
         */
        
        /*
        /// <summary>
        /// Populate field or array. Enter the name of the field to be filled, use Populate attribute above the field containing the references
        /// </summary>
        /// <param name="targetField">The field to be filled</param>
        public void Populate(string targetField)
            => ChangePopulate(targetField, true);

        /// <summary>
        /// Depopulate field or array. Use the name of the target field
        /// </summary>
        /// <param name="targetField">The field to be depopulated</param>
        public void Depopulate(string targetField)
            => ChangePopulate(targetField, false);

        /// <summary>
        /// Populate field by hand (without populate attribute)
        /// </summary>
        /// <param name="otherCollection">The other collection's name, i.e "users"</param>
        /// <param name="targetField">The field to be filled (in current collection)</param>
        /// <param name="localField">The field in current collection containing the references</param>
        /// <param name="isCollection">Indicates if target field is array or collection. Defaults to true</param>
        /// <param name="otherField">The key field in the other collection, default _id</param>
        public void Populate(string otherCollection, string targetField, string localField, bool isCollection = true,
            string otherField = "_id")
        {
            if (_populates.All(x => x.TargetField != targetField))
                _populates.Add(new Populate
                {
                    RefCollection = otherCollection, TargetField = targetField, LocalField = localField,
                    IsObject = !isCollection, RefField = otherField, On = true, Level = targetField.Count(x => x == '.')
                });
            else
                Populate(targetField);
        }*/
        
        /*

        /// <summary>
        /// Find item by id
        /// </summary>
        /// <param name="id">Item's ID</param>
        /// <returns>The item, or null if not found</returns>
        public T FindById(ObjectId id)
            => BuildAggregate().Match(x => x.Id == id).Limit(1).FirstOrDefault();
        
        public T FindById(string id)
            => FindById(new ObjectId(id));

        /// <summary>
        /// Enumerate all items in collection
        /// </summary>
        /// <returns>IEnumerable</returns>
        public IEnumerable<T> All()
            => BuildAggregate().ToEnumerable();

        /// <summary>
        /// Get first item in collection
        /// </summary>
        /// <returns>First item in collection, or null if collection is empty</returns>
        public T First()
            => BuildAggregate().Limit(1).FirstOrDefault();

        /// <summary>
        /// Enumerate all items with limit
        /// </summary>
        /// <param name="limit">The limit</param>
        /// <returns>IEnumerable</returns>
        public IEnumerable<T> All(int limit)
            => BuildAggregate().Limit(limit).ToEnumerable();*/
        
        
        /// <summary>
        /// Insert one or more documents into collection
        /// </summary>
        /// <param name="items">The item(s) to be inserted</param>
        public async Task Insert(params T[] items)
            => await _collection.InsertManyAsync(items/*.Select(DepopulateHook)*/);

        /// <summary>
        /// Update document
        /// </summary>
        /// <param name="item">The document to be updated</param>
        public async Task Update(T item)
            => await _collection.ReplaceOneAsync(x => x.Id == item.Id, item/*DepopulateHook(item)*/);

        /// <summary>
        /// Delete existing item
        /// </summary>
        /// <param name="item">The item to be deleted</param>
        public async Task Delete(T item) 
            => await Delete(item.Id);
        
        public async Task Delete(string id) 
            => await Delete(new ObjectId(id));

        /// <summary>
        /// Delete item by id
        /// </summary>
        /// <param name="id">The item to be deleted</param>
        public async Task Delete(ObjectId id)
            => await _collection.DeleteOneAsync(b => b.Id == id);        
        
        /// <summary>
        /// Count all documents
        /// </summary>
        /// <returns>long</returns>
        public async Task<long> Count()
            => await Count(Builders<T>.Filter.Empty);
        
        /*
         * Documents without any special functionality or guarantees
         */
        
        /// <summary>
        /// Update documents by id (raw)
        /// </summary>
        /// <param name="id">The id</param>
        /// <param name="update">update definition</param>
        public async Task Update(string id, UpdateDefinition<T> update)
            => await Update(new ObjectId(id), update);

        /// <summary>
        /// Update documents by id (raw)
        /// </summary>
        /// <param name="id">The id</param>
        /// <param name="update">update definition</param>
        public async Task Update(ObjectId id, UpdateDefinition<T> update)
            => await _collection.UpdateOneAsync(GetFilter(id), update);

        /// <summary>
        /// Update documents by filter (raw)
        /// </summary>
        /// <param name="filter">The update filter</param>
        /// <param name="update">update definition</param>
        public async Task Update(FilterDefinition<T> filter, UpdateDefinition<T> update)
            => await _collection.UpdateManyAsync(filter, update);

        
        /// <summary>
        /// Delete documents by filter (raw)
        /// </summary>
        /// <param name="filter">The filter</param>
        public async Task Delete(FilterDefinition<T> filter)
            => await _collection.DeleteManyAsync(filter);

        
        /// <summary>
        /// Count documents by filter (raw)
        /// </summary>
        /// <param name="filter">The filter</param>
        /// <returns>long</returns>
        public async Task<long> Count(FilterDefinition<T> filter)
            => await _collection.CountDocumentsAsync(filter);
    }
}