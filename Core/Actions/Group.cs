using System.Collections.Generic;
using Core.Abstractions;

namespace Core.Actions
{
    internal class Group : IMapperAction
    {
        public string Path { get; set; }
        public bool Enabled { get; set; }
        public bool IsCollection { get; set; }
        public List<string> Fields { get; set; }
        public List<string> PushFields { get; set; }

        public override string ToString()
            => $"GROUP {Path} ({(Enabled ? "On" : "Off")}{(IsCollection ? ", Collection" : "")})";
    }
}