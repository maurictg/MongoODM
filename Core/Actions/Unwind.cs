using Core.Abstractions;

namespace Core.Actions
{
    internal class Unwind : IMapperAction
    {
        public string Path { get; set; }
        public bool Enabled { get; set; }
        public bool IsCollection { get; set; }

        public override string ToString()
            => $"UNWIND {Path} ({(Enabled ? "On" : "Off")}{(IsCollection?", Collection":"")})";
    }
}