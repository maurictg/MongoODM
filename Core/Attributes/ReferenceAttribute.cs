using System;
using Core.Abstractions;
using MongoDB.Bson.Serialization.Attributes;

namespace Core.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ReferenceAttribute : BsonIgnoreIfNullAttribute, IMongoAttribute
    {
        /// <summary>
        /// The referenced collection
        /// </summary>
        public string RefCollection { get; set; }
        
        /// <summary>
        /// The referenced field. Defaults to _id
        /// </summary>
        public string RefField { get; set; }
        
        /// <summary>
        /// The local field containing the Id's
        /// </summary>
        public string LocalField { get; set; }
        
        /// <summary>
        /// Indicates if mongoODM has to auto populate this field. Defaults to false
        /// </summary>
        public bool AutoPopulate { get; set; }

        /// <summary>
        /// Create new reference attribute
        /// </summary>
        /// <param name="refCollection">The referenced collection</param>
        /// <param name="localField">The local field containing the Id's</param>
        /// <param name="refField">The referenced field. Defaults to _id</param>
        /// <param name="autoPopulate">Indicates if mongoODM has to auto populate this field. Defaults to false</param>
        public ReferenceAttribute(string refCollection, string localField, string refField = "_id", bool autoPopulate = false)
        {
            RefCollection = refCollection;
            LocalField = localField;
            RefField = refField;
            AutoPopulate = autoPopulate;
        }
    }
}