using Core;
using Core.Abstractions;

namespace Cli
{
    public class Product : Entity
    {
        public string Name { get; set; }
        public int Price { get; set; }
    }
}