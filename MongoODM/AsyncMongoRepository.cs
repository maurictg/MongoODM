namespace MongoODM
{
    /*
    public class AsyncMongoRepository<T> : MongoRepository<T>, IAsyncMongoRepository<T> where T : Entity
    {
        public AsyncMongoRepository(IMongoDatabase database, string collection = null) : base(database, collection)
        {
        }

        /// <summary>
        /// Add a document to the database and fill the document's ID
        /// </summary>
        /// <param name="document">The document to be added</param>
        public async Task InsertAsync(T document)
        {
            var doc = DepopulateHook(document);
            await Collection.InsertOneAsync(doc);
            document.Id = doc.Id;
        }
    }
    */
}