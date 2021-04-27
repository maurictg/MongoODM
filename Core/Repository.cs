using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.Abstractions;
using Core.Attributes;
using Core.Helpers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Core
{
    public class Repository<T> where T : Entity
    {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<T> _collection;

        //Methods used for reflection part
        private readonly MethodInfo _castMethod;
        private readonly MethodInfo _toListMethod;
        private readonly MethodInfo _toArrayMethod;
        private readonly MethodInfo _deserializeMethod;

        /// <summary>
        /// Create new repository
        /// </summary>
        /// <param name="database">The database used</param>
        /// <param name="collection">The name of the collection, defaults to the name of the generic type in snake_case</param>
        /// <exception cref="ArgumentException"></exception>
        public Repository(IMongoDatabase database, string collection = null)
        {
            collection ??= typeof(T).Name.ToSnakeCase();
            _database = database;
            _collection = _database.GetCollection<T>(collection);

            //Get reflection methods
            _castMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.Cast)) ??
                          throw new ArgumentException("Failed to get cast method");

            _toListMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.ToList)) ??
                            throw new ArgumentException("Failed to get toList method");

            _toArrayMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.ToArray)) ??
                             throw new ArgumentException("Failed to get toArray method");

            _deserializeMethod = typeof(BsonSerializer).GetMethods()
                .Where(x => x.Name == nameof(BsonSerializer.Deserialize))
                .First(x => x.IsGenericMethod);
        }

        /// <summary>
        /// The population logic. Is called recursively over all properties and is used to lookup and fill enabled references
        /// </summary>
        /// <param name="t">The type to be evaluated</param>
        /// <param name="value">The instance to be filled</param>
        private void PopulateHook(Type t, object value)
        {
            Console.WriteLine("![T]! Checking " + t.Name);
            foreach (var prop in t.GetProperties())
            {
                bool skip = false;
                bool index = false;

                Console.WriteLine("[P] Property " + prop.Name);
                var val = prop.GetValue(value);
                var propType = prop.PropertyType;
                
                //Check attributes
                foreach (var attr in prop.GetCustomAttributes(true))
                {
                    //If embed or reference attribute, make sure to index 
                    if (attr is IMongoAttribute)
                        index = true;

                    //If reference attribute, check if it is enabled
                    if (attr is ReferenceAttribute refAttr)
                    {
                        //If not enabled, skip whole propery (ignore everything)
                        if (!refAttr.Enabled)
                        {
                            Console.WriteLine("[XREF] Found disabled referenceAttribute, skipping");
                            skip = true;
                            break;
                        }

                        //If the propery is already filled, we assume it is already populated
                        if (val != null)
                            continue;

                        Console.WriteLine("[REF] Found enabled referenceAttribute on " + prop.Name);

                        //Get values
                        var refFieldValue = Tools.GetValue(refAttr.LocalField, value);
                        
                        //If no references are present, ignore and skip
                        if (refFieldValue == null)
                        {
                            Console.WriteLine("[XREF] No references are present");
                            skip = true;
                            break;
                        }

                        //Convert ref(s) to ICollection of objectIds
                        var refs = !refFieldValue.GetType().IsCollection(out _)
                            ? new List<ObjectId> {(ObjectId) refFieldValue}
                            : (ICollection<ObjectId>) refFieldValue;

                        //Lookup document
                        var refCollection = _database.GetCollection<BsonDocument>(refAttr.RefCollection);
                        var filter = Builders<BsonDocument>.Filter.In(refAttr.RefField, refs);
                        var query = refCollection.Find(filter).ToList();

                        //Get output type
                        var outputType = propType;
                        var isArray = outputType.IsArray;
                        var isCollection = outputType.IsCollection(out outputType);

                        //Deserialize results
                        var deserialize = _deserializeMethod.MakeGenericMethod(outputType);
                        var queryResults = query.Select(x =>
                            deserialize.Invoke(null, new object[] {x, null})).ToList();
                        
                        //Convert results to desired type
                        var cast = _castMethod.MakeGenericMethod(outputType);
                        var castResults = cast.Invoke(null, new[] {queryResults});

                        var toList = _toListMethod.MakeGenericMethod(outputType);
                        var results = toList.Invoke(null, new[] {castResults});

                        //Check if the output type is an collection. If so, fill with array or list value
                        if (isCollection)
                        {
                            Console.WriteLine("Set value to " + prop.Name + ", is " + results);
                            if (isArray)
                            {
                                var toArray = _toArrayMethod.MakeGenericMethod(outputType);
                                prop.SetValue(value, toArray.Invoke(null, new[] {results}));
                            }
                            else
                            {
                                prop.SetValue(value, results);
                            }
                        }
                        else
                        {
                            //If not, get first item (always 1, we assume) and fill it
                            Console.WriteLine("Set value to " + prop.Name + ", is " + results);
                            var res = ((IList) results)?[0];
                            prop.SetValue(value, res);
                        }

                        val = prop.GetValue(value);
                    }
                }

                Console.WriteLine("Property " + prop.Name + " with value " + (val ?? "NULL"));

                //If skip is triggered, value is still null or value is not needed to be indexed skip property
                if (skip || val == null || !index)
                {
                    Console.WriteLine("[SKIP] Skipping " + prop.Name);
                    continue;
                }

                //Check if property is collection. If so, run populate hook over every item in the collection
                if (propType.IsCollection(out t))
                {
                    Console.WriteLine("Collection containing " + t.Name);
                    var cast = typeof(Enumerable).GetMethod(nameof(Enumerable.Cast))?.MakeGenericMethod(t);
                    var casted = cast?.Invoke(null, new[] {val});
                    if (casted != null)
                    {
                        foreach (var c in (IEnumerable) casted)
                        {
                            PopulateHook(t, c);
                        }
                    }
                    else
                    {
                        Console.WriteLine("[E] CASTED IS NULL!");
                    }
                }
                else //If not, run populate hook over object
                {
                    Console.WriteLine("Type is Entity, index");
                    PopulateHook(t, val);
                }
            }
        }

        /// <summary>
        /// Depopulation hook, used to remove the populated values from an object
        /// </summary>
        /// <param name="t">The type to be evaluated</param>
        /// <param name="value">The value to be set</param>
        private void DepopulateHook(Type t, object value)
        {
            foreach (var prop in t.GetProperties())
            {
                foreach (var attr in prop.GetCustomAttributes())
                {
                    switch (attr)
                    {
                        case EmbedAttribute:
                            DepopulateHook(prop.PropertyType, prop.GetValue(value));
                            break;
                        case ReferenceAttribute:
                            prop.SetValue(value, null);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Shorthand for the population hook
        /// </summary>
        /// <param name="value">The value to be populated</param>
        private void PopulateHook(T value)
            => PopulateHook(typeof(T), value);

        /// <summary>
        /// Shorthand for the depopulation hook.
        /// </summary>
        /// <param name="value">The value to be depopulated</param>
        /// <returns>Depopulated value</returns>
        public T DepopulateHook(T value)
        {
            var val = value.Clone();
            DepopulateHook(typeof(T), val);
            return (T)val;
        }

        public T First()
        {
            var first = _collection.Find(x => true).First();
            PopulateHook(first);
            return first;
        }
    }
}