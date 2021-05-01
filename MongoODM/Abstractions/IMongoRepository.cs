using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoODM.Abstractions
{
    /// <summary>
    /// The instance base of the MongoRepository. You can create your own implementation using this interface, or use this for DI
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    public interface IMongoRepository<T> where T : IEntity
    {
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
        /// </summary>
        public bool UseLookup { get; set; }

        /// <summary>
        /// Indicates if you want to allow MongoODM to populate user-defined fields, decorated by attributes like [Embed] or [Reference]
        /// </summary>
        public bool DoPopulate { get; set; }
        
        /// <summary>
        /// Indicates if you want to automatically convert list of objects to list of references in the depopulate mechanism.
        /// Currently you must enable it manually to use this feature
        /// </summary>
        public bool UseDepopulate { get; set; }


        /// <summary>
        /// Override a specific reference attribute to be enabled in population
        /// </summary>
        /// <param name="paths">The object(s) to be populated</param>
        public void Populate(params string[] paths);

        /// <summary>
        /// Override a specific reference attribute to be disabled in population
        /// </summary>
        /// <param name="paths">The object to be disabled</param>
        public void Depopulate(params string[] paths);

        /// <summary>
        /// Reset all populate attribute overrides
        /// </summary>
        public void ResetPopulates();
        
        /*
         * CRUD methods
         */

        /// <summary>
        /// Creates a filter for one item's ID
        /// </summary>
        /// <param name="id">The id to be filtered</param>
        /// <returns>Filter</returns>
        public FilterDefinition<T> GetFilter(string id);

        //Create filters using JSON, BSON, expression or an object
        private FilterDefinition<T> CreateFilter(object obj)
            => new ObjectFilterDefinition<T>(obj);
        private FilterDefinition<T> CreateFilter(string json)
            => new JsonFilterDefinition<T>(json);
        private FilterDefinition<T> CreateFilter(Expression<Func<T, bool>> filter)
            => new ExpressionFilterDefinition<T>(filter);
        private FilterDefinition<T> CreateFilter(BsonDocument filter)
            => new BsonDocumentFilterDefinition<T>(filter);
        
        //Create updates using JSON, BSON or an object
        private UpdateDefinition<T> CreateUpdate(object obj)
            => new ObjectUpdateDefinition<T>(obj);
        private UpdateDefinition<T> CreateUpdate(string json)
            => new JsonUpdateDefinition<T>(json);
        
        private UpdateDefinition<T> CreateUpdate(BsonDocument filter)
            => new BsonDocumentUpdateDefinition<T>(filter);

        /// <summary>
        /// Adds a document to the collection
        /// </summary>
        /// <param name="document">The document to be added</param>
        public void Insert(T document);

        /// <summary>
        /// Add multiple documents to the collection
        /// </summary>
        /// <param name="documents">The documents to be added</param>
        public void InsertMany(params T[] documents);

        /// <summary>
        /// Get first item from collection matching a filter
        /// </summary>
        /// <param name="filter">The filter to apply</param>
        /// <returns>The first element matching the filter or NULL</returns>
        public T First(FilterDefinition<T> filter = null);

        /// <summary>
        /// Get first item in collection matching BsonDocument filter
        /// </summary>
        /// <param name="filter">The filter to apply</param>
        /// <returns>The first item matching the filter or NULL</returns>
        public T First(BsonDocument filter) => First(CreateFilter(filter));

        /// <summary>
        /// Get first document matching a filter expression
        /// </summary>
        /// <param name="filter">The filter to apply</param>
        /// <returns>The first document in the collection matching the filter or NULL</returns>
        public T First(Expression<Func<T, bool>> filter) => First(CreateFilter(filter));

        public T First(object filter) => First(CreateFilter(filter));
        public T First(string jsonFilter) => First(CreateFilter(jsonFilter));

        /// <summary>
        /// Get item by its Id
        /// </summary>
        /// <param name="id">The id to search for</param>
        /// <returns>The item matching the id or null if not found</returns>
        public T FindById(string id)
            => First(GetFilter(id));

        /// <summary>
        /// Get all elements in collection matching a filter
        /// </summary>
        /// <param name="filter">The filter to check</param>
        /// <returns>All elements matching filter</returns>
        public IEnumerable<T> Find(FilterDefinition<T> filter = null);

        /// <summary>
        /// Get all documents in collection matching BsonDocument filter
        /// </summary>
        /// <param name="filter">The filter to apply</param>
        /// <returns>The documents matching the filter</returns>
        public IEnumerable<T> Find(BsonDocument filter) => Find(CreateFilter(filter));

        /// <summary>
        /// Get all documents in collection matching a filter expression
        /// </summary>
        /// <param name="filter">The filter to apply</param>
        /// <returns>The documents matching the filter</returns>
        public IEnumerable<T> Find(Expression<Func<T, bool>> filter) => Find(CreateFilter(filter));
        
        public IEnumerable<T> Find(object filter) => Find(CreateFilter(filter));
        public IEnumerable<T> Find(string jsonFilter) => Find(CreateFilter(jsonFilter));

        /// <summary>
        /// Count all documents in collection matching optional filter
        /// </summary>
        /// <param name="filter">Optional: filter</param>
        /// <returns>The amount of documents matching a filter</returns>
        public long Count(FilterDefinition<T> filter = null);

        /// <summary>
        /// Count documents matching a filter
        /// </summary>
        /// <param name="filter">The filter to apply</param>
        /// <returns>The amount of documents matching the filter</returns>
        public long Count(Expression<Func<T, bool>> filter) => Count(CreateFilter(filter));

        /// <summary>
        /// Count documents matching a BsonDocument filter
        /// </summary>
        /// <param name="filter">The filter to apply</param>
        /// <returns>The amount of documents matching the filter</returns>
        public long Count(BsonDocument filter) => Count(CreateFilter(filter));
        
        public long Count(object filter) => Count(CreateFilter(filter));
        public long Count(string jsonFilter) => Count(CreateFilter(jsonFilter));

        /// <summary>
        /// Update document
        /// </summary>
        /// <param name="document">The document to be updated</param>
        /// <returns>The updated document</returns>
        public T Update(T document);

        /// <summary>
        /// Update one or more fields by Id
        /// </summary>
        /// <param name="id">The object to be updated</param>
        /// <param name="update">The update to apply</param>
        /// <returns>True if update is acknowledged</returns>
        public bool Update(string id, UpdateDefinition<T> update);
        
        /// <summary>
        /// Update one or more fields by an object
        /// </summary>
        /// <param name="id">The item to be updated</param>
        /// <param name="update">The update definition object</param>
        /// <returns>True if update is acknowledged</returns>
        public bool Update(string id, object update) => Update(id, CreateUpdate(update));
        public bool Update(string id, string json) => Update(id, CreateUpdate(json));
        public bool Update(string id, BsonDocument update) => Update(id, CreateUpdate(update));

        /// <summary>
        /// Update many documents using filter and updateDefinition
        /// </summary>
        /// <param name="filter">The filter to apply</param>
        /// <param name="update">The update</param>
        /// <returns>UpdateResult</returns>
        public UpdateResult UpdateMany(FilterDefinition<T> filter, UpdateDefinition<T> update);

        /// <summary>
        /// Delete an object by its id
        /// </summary>
        /// <param name="id">The id of the object to be deleted</param>
        /// <returns>True if delete is acknowledged</returns>
        public bool Delete(string id);

        /// <summary>
        /// Delete a document from the database
        /// </summary>
        /// <param name="item">The document to delete</param>
        /// <returns>True if delete is acknowledged</returns>
        public bool Delete(T item)
            => Delete(item.Id);

        /// <summary>
        /// Delete all documents matching a filter
        /// </summary>
        /// <param name="filter">The filter to look for</param>
        /// <returns>The amount of deleted documents. -1 if failed</returns>
        public int DeleteMany(FilterDefinition<T> filter);

        /// <summary>
        /// Delete all documents matching a filter
        /// </summary>
        /// <param name="filter">The filter to look for</param>
        /// <returns>The amount of deleted documents. -1 if failed</returns>
        public int DeleteMany(Expression<Func<T, bool>> filter) => DeleteMany(CreateFilter(filter));

        /// <summary>
        /// Delete all documents matching a BsonDocument filter
        /// </summary>
        /// <param name="filter">The filter to look for</param>
        /// <returns>The amount of deleted documents. -1 if failed</returns>
        public int DeleteMany(BsonDocument filter) => DeleteMany(CreateFilter(filter));
        public int DeleteMany(string jsonFilter) => DeleteMany(CreateFilter(jsonFilter));
    }
}