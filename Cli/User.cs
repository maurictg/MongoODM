using System;
using System.Collections.Generic;
using Core;
using Core.Attributes;
using MongoDB.Bson;

namespace Cli
{
    public class User : Entity
    {
        public string Name { get; set; }
        
        [Embed]
        public List<Usernames> Usernames { get; set; }
        
        [Reference("orders", "OrderRefs")]
        public List<Order> Orders { get; set; }
        public List<ObjectId> OrderRefs { get; set; }
    }
}