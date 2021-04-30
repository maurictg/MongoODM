using System.Collections.Generic;
using System.Linq;
using Core;
using Core.Attributes;

namespace Cli
{
    public class Email : Entity
    {
        [Embed] //Group is incorrect
        public List<Domain> Domains { get; set; }
        public string Address { get; set; }
    }
}