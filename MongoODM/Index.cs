namespace MongoODM
{
    public class Index
    {
        /// <summary>
        /// The index type
        /// </summary>
        public IndexType Type { get; set; } 
        
        /// <summary>
        /// The field to create the index on
        /// </summary>
        public string Field { get; set; }
    }
}