using System;
using System.Collections.Generic;
using Core;
using Core.Attributes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Cli
{
    public class User : Entity
    {
        public string Name { get; set; }
        
        [Embed]
        public List<Usernames> Usernames { get; set; }
        
        [Reference("orders", "OrderRefs", autoPopulate: true)]
        public Order[] Orders { get; set; }
        
        [BsonRepresentation(BsonType.ObjectId)]
        public List<string> OrderRefs { get; set; }
        
        [Reference("products", "CartRefs", autoPopulate:true)]
        public List<Product> ShoppingCart { get; set; }
        
        public List<string> CartRefs { get; set; }
    }
}