using System.Collections.Generic;
using Core;
using Core.Attributes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Cli
{
    public class Order : Entity
    {
        public string Name { get; set; }
        
        [Reference("products", "ProductRefs", autoPopulate: true)]
        public List<Product> Products { get; set; }
        
        [BsonRepresentation(BsonType.ObjectId)]
        public List<string> ProductRefs { get; set; }
    }
}