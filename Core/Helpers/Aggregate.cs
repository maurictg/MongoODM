using System;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Core.Helpers
{
    internal static class AggregateHelper
    {
        /*internal static IAggregateFluent<BsonDocument> BuildGroup(this IAggregateFluent<BsonDocument> a, Populate p)
        {
            var doc = new BsonDocument("_id", "$_id");
            
            //1. get parent properties, not id or name of target collection
            var target = p.TargetField.Split('.')[0];
            Console.WriteLine(target);

            var props = p.ParentType.GetProperties().Where(x => x.Name != "Id" && x.Name != target)
                .Select(x => (x.Name.ToCamelCase(), new BsonDocument("$first", "$" + x.Name.ToCamelCase())))
                .ToDictionary(x => x.Item1, y => y.Item2);
            doc.AddRange(props);
            doc.Add(target.ToCamelCase(), new BsonDocument("$push", "$" + target.ToCamelCase()));
            
            return a.Group(doc);
        }

        internal static IAggregateFluent<BsonDocument> BuildLookup(this IAggregateFluent<BsonDocument> a, Populate p)
        {
            a = a.Lookup(p.RefCollection, p.LocalField, p.RefField, p.TargetField);
                
            if (p.IsObject) //unwind array to object
                a = a.Unwind(p.TargetField);
            return a;
        }*/
    }
}