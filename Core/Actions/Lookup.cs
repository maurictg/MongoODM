using Core.Abstractions;

namespace Core.Actions
{
    internal class Lookup : MapperAction
    {
        /// <summary>
        /// The field containing the references
        /// </summary>
        public string LocalField { get; set; }
        
        /// <summary>
        /// The collection containing the elements
        /// </summary>
        public string RefCollection { get; set; }
        
        /// <summary>
        /// The field, matching the local field
        /// </summary>
        public string RefField { get; set; }

        public override string ToString()
            =>base.ToString()+$" Local: {LocalField}, RefCollection: {RefCollection}, RefField: {RefField}";
    }
}