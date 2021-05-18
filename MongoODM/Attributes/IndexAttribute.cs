using System;
using System.Collections.Generic;

namespace MongoODM.Attributes
{
    public class IndexAttribute : Attribute
    {
        /// <summary>
        /// The index fields the attribute contains
        /// </summary>
        public Index[] Indexes { get; set; }
        
        /// <summary>
        /// Indicates if index is unique
        /// </summary>
        public bool Unique { get; set; }
        
        public IndexAttribute(bool unique, params Index[] indexes)
        {
            Unique = unique;
            Indexes = indexes;
        }

        public IndexAttribute(string field, IndexType type = IndexType.Descending, bool unique = true)
        {
            Indexes = new[] {new Index() {Field = field, Type = type}};
            Unique = unique;
        }
    }
}