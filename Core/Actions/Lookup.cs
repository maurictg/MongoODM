using Core.Abstractions;

namespace Core.Actions
{
    internal class Lookup : IMapperAction
    {
        public string Path { get; set; }
        public bool Enabled { get; set; }
        public bool IsCollection { get; set; }
        public string LocalField { get; set; }
        public string RefCollection { get; set; }
        public string RefField { get; set; }

        public override string ToString()
            =>$"LOOKUP {Path} ({(Enabled ? "On" : "Off")}{(IsCollection?", Collection":"")})"+ $" Local: {LocalField}, RefCollection: {RefCollection}, RefField: {RefField}";
    }
}