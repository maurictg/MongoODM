using System;
using Core.Abstractions;

namespace Core.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class EmbedAttribute : Attribute, IMongoAttribute
    {
        
    }
}