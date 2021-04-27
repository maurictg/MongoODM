using System.Linq;

namespace Core.Abstractions
{
    internal abstract class MapperAction
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

        /// <summary>
        /// Get level of action
        /// </summary>
        public int Level => Path.Count(x => x == '.');

        public override string ToString()
            => $"({GetType().Name.ToUpper()}: {(Enabled?"On":"Off")}) @{Path}, {(IsCollection?"Collection":"")} ";
    }
}