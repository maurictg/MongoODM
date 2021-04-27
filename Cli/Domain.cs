using Core.Abstractions;
using Core.Attributes;
using MongoDB.Bson;

namespace Cli
{
    public class Domain
    {
        public string Name { get; set; }
        
        [Reference("companies", "CompanyRef", autoPopulate: true)]
        public Company Company { get; set; }
        
        public ObjectId CompanyRef { get; set; }
    }
}