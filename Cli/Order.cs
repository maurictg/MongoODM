using System.Collections.Generic;
using Core;
using Core.Attributes;
using MongoDB.Bson;

namespace Cli
{
    public class Order : Entity
    {
        public string Name { get; set; }
        
        [Reference("products", "ProductRefs")]
        public List<Product> Products { get; set; }
        public List<ObjectId> ProductRefs { get; set; }
    }
}