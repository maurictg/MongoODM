using System.Collections.Generic;
using Core;
using Core.Abstractions;
using Core.Attributes;
using MongoDB.Bson;

namespace Cli
{
    public class Usernames
    {
        public string Names { get; set; }
        public int Amount { get; set; }
        
        [Reference("emails", "EmailRefs", autoPopulate: true)]
        public List<Email> Emails { get; set; }
        public List<ObjectId> EmailRefs { get; set; }
    }
}