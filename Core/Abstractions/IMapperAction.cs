namespace Core.Abstractions
{
    internal interface IMapperAction
    {
        /// <summary>
        /// The path of the field containing the action
        /// </summary>
        public string Path { get; set; }
        
        /// <summary>
        /// Indicate if attribute is enabled
        /// </summary>
        public bool Enabled { get; set; }
        
        /// <summary>
        /// Indicate if path is collection
        /// </summary>
        public bool IsCollection { get; set; }
    }
}