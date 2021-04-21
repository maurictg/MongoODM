using System;
using System.Collections;
using System.Linq;

namespace Core.Helpers
{
    internal static class Tools
    {
        /// <summary>
        /// Get value in path in object. Does not support arrays/collections
        /// </summary>
        /// <param name="path">The path</param>
        /// <param name="obj">The object</param>
        /// <returns>The value at the specific path</returns>
        public static object GetValue(string path, object obj)
        {
            var paths = path.Split('.');
            var t = obj.GetType();
            var attr = t.GetProperty(paths[0]);
            if (attr != null)
                return paths.Length > 1
                    ? GetValue(string.Join('.', paths[1..]), attr.GetValue(obj))
                    : attr.GetValue(obj);
            
            Console.WriteLine("[E] ATTR is null!");
            return null;
        }

        /// <summary>
        /// Checks if type is an array or collection of a specific type
        /// </summary>
        /// <param name="check">The type</param>
        /// <param name="type">The type (or abstraction) to be checked</param>
        /// <param name="elementType">The actual type</param>
        /// <returns>True when type is array or collection of specific type</returns>
        public static bool IsCollectionOf(this Type check, Type type, out Type elementType)
            => check.IsCollection(out elementType) && elementType.IsAssignableTo(type);
        
        /// <summary>
        /// Checks if item is collection and return collection type
        /// </summary>
        /// <param name="check">The type to be checked</param>
        /// <param name="elementType">The element found</param>
        /// <returns>True if item is collection</returns>
        public static bool IsCollection(this Type check, out Type elementType)
        {
            elementType = null;
            if (check.IsGenericType && check.IsAssignableTo(typeof(ICollection)))
            {
                var arg = check.GetGenericArguments()[0];
                elementType = arg;
                return true;
            }

            if (!check.IsArray) 
                return false;
            
            elementType = check.GetElementType();
            return true;
        }
        
        

        /// <summary>
        /// Convert string to camelCase
        /// </summary>
        /// <param name="input">The PascalCase string</param>
        /// <returns>camelCase string</returns>
        public static string ToCamelCase(this string input)
        {
            return string.IsNullOrEmpty(input) || input.Length < 2
                ? input
                : char.ToLowerInvariant(input[0]) + input.Substring(1);
        }

        /// <summary>
        /// Set value to specific path in object. Ignores everything that does not exist, also handles arrays/collections.
        /// So: MyArray.ArrayValue will set all ArrayValues in MyArray
        /// </summary>
        /// <param name="path">The path in the object</param>
        /// <param name="obj">The object to be modified</param>
        /// <param name="data">The data to be set</param>
        public static void SetValues(string path, object obj, object data)
        {
            if (obj == null)
                return;
            
            var paths = path.Split('.');
            var t = obj.GetType();
            
            var attr = t.GetProperty(paths[0]);
            if (attr == null)
                return;

            if (paths.Length > 1)
            {
                if (attr.PropertyType.IsCollectionOf(typeof(object), out _)) //TODO type
                {
                    var value = attr.GetValue(obj) as IEnumerable;
                    if (value == null)
                        return;

                    foreach (var item in value)
                        SetValues(string.Join('.',paths[1..]), item, data);
                }
                
                if (attr.PropertyType.IsAssignableTo(typeof(object)))
                {
                    var item = attr.GetValue(obj);
                    SetValues(string.Join('.',paths[1..]), item, data);
                }
            }
            else
            {
                //Set value
                attr.SetValue(obj, data);
            }
        }
    }
}