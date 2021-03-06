using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoODM.Abstractions;

namespace MongoODM
{
    /// <summary>
    /// The mongoDB entity class
    /// Contains ID. When creating Embedded class, use IEntity
    /// </summary>
    [BsonIgnoreExtraElements]
    public abstract class Entity : IEntity
    {
        /// <summary>
        /// Represents MongoDB _id
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        /// <summary>
        /// Gets the mongoDb ObjectId instead of the string value
        /// </summary>
        [BsonIgnore] 
        [JsonIgnore]
        public ObjectId ObjectId => new(Id);

        /// <summary>
        /// Deep clone entity, thanks to serialization
        /// </summary>
        /// <returns>Deep cloned entity</returns>
        public IEntity Clone()
            => BsonSerializer.Deserialize<Entity>(this.ToBson());
    }
}