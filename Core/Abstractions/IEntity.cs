namespace Core.Abstractions
{
    /// <summary>
    /// The Entity base class
    /// </summary>
    public interface IEntity
    {
        /// <summary>
        /// Represents MongoDB _id
        /// Must be decorated with the [BsonRepresentation(BsonType.ObjectId)] and [BsonId] attribute
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Deep clone the entity instance
        /// </summary>
        /// <returns>The cloned instance</returns>
        public IEntity Clone();
    }
}