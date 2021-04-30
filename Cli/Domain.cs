using Core.Abstractions;
using Core.Attributes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Cli
{
    public class Domain
    {
        public string Name { get; set; }
        
        [Reference("companies", "CompanyRef", autoPopulate: true)]
        public Company Company { get; set; }
        
        [BsonRepresentation(BsonType.ObjectId)]
        public string CompanyRef { get; set; }
    }
}