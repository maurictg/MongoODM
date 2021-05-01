using System;
using MongoODM.Abstractions;

namespace MongoODM.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class EmbedAttribute : Attribute, IMongoAttribute
    {
        
    }
}