using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Core.Helpers
{
    internal static class ToolsOld
    {
        /// <summary>
        /// Get value in path in object. Does not support arrays/collections
        /// </summary>
        /// <param name="path">The path</param>
        /// <param name="obj">The object</param>
        /// <returns>The value at the specific path</returns>
        public static object GetValue(string path, object obj, Type t = null)
        {
            var paths = path.Split('.');
            t ??= obj.GetType();
            var attr = t.GetProperty(paths[0]);
            if (attr != null)
                return paths.Length > 1
                    ? GetValue(string.Join('.', paths[1..]), attr.GetValue(obj))
                    : attr.GetValue(obj);
            
            Console.WriteLine("[E] ATTR is null!");
            return null;
        }
        
        /// <summary>
        /// Get type of path in object. Does not support arrays/collections
        /// </summary>
        /// <param name="path">The path</param>
        /// <param name="t">The object type</param>
        /// <returns>The value at the specific path</returns>
        public static Type GetType(string path, Type t)
        {
            var paths = path.Split('.');
            var attr = t.GetProperty(paths[0]);
            if (attr != null)
                return paths.Length > 1
                    ? GetType(string.Join('.', paths[1..]), attr.PropertyType)
                    : attr.PropertyType;
            
            Console.WriteLine("[E] ATTR is null!");
            return null;
        }

        /*
        
        /// <summary>
        /// Checks if item is collection and return collection type
        /// </summary>
        /// <param name="check">The type to be checked</param>
        /// <param name="elementType">The element found</param>
        /// <returns>True if item is collection</returns>
        public static bool IsCollection(this Type check, out Type elementType)
        {
            elementType = check;
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
        }*/

        /// <summary>
        /// Get last part of path string
        /// </summary>
        /// <param name="path">Path, like test.a.path</param>
        /// <returns>The last part of a path. In case of the example: "path"</returns>
        public static string LastPart(this string path)
        {
            var idx = path.LastIndexOf('.');
            if (idx == -1)
                idx = 0;
            return path[idx..];
        }
        
        public static string FirstPart(this string path)
        {
            var idx = path.IndexOf('.');
            if (idx == -1)
                idx = 0;
            return path[..idx];
        }
        
        public static object ConvertList(IEnumerable<object> items, Type type, bool performConversion = false)
        {
            var containedType = type;
            var enumerableType = typeof(System.Linq.Enumerable);
            var castMethod = enumerableType.GetMethod(nameof(System.Linq.Enumerable.Cast)).MakeGenericMethod(containedType);
            var toListMethod = enumerableType.GetMethod(nameof(System.Linq.Enumerable.ToList)).MakeGenericMethod(containedType);

            IEnumerable<object> itemsToCast;

            if(performConversion)
            {
                itemsToCast = items.Select(item => Convert.ChangeType(item, containedType));
            }
            else 
            {
                itemsToCast = items;
            }

            var castedItems = castMethod.Invoke(null, new[] { itemsToCast });

            return toListMethod.Invoke(null, new[] { castedItems });
        }
       
    }
}