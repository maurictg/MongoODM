using System;
using System.Linq;

namespace Core.Properties
{
    internal class AggregateSettings
    {
        public bool DoNest { get; set; } = true;
        public bool IsCollection { get; set; } = false;
        public int Level => Path.Count(x => x == '.');
        public Type ParentType { get; set; } = null;
        public string Path { get; set; } = "";
        public string GetPath(string propName) => Path + propName;
    }
}