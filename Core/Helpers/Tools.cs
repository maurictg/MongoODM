using System;
using System.Collections;
using System.Linq;
using System.Text;

namespace Core.Helpers
{
    internal static class Tools
    {
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
        /// Checks if type is an array or collection of a specific type
        /// </summary>
        /// <param name="check">The type</param>
        /// <param name="type">The type (or abstraction) to be checked</param>
        /// <param name="elementType">The actual type</param>
        /// <returns>True when type is array or collection of specific type</returns>
        public static bool IsCollectionOf(this Type check, Type type, out Type elementType)
            => check.IsCollection(out elementType) && elementType.IsAssignableTo(type);
        
        /// <summary>
        /// Converts string in PascalCase, snake_case or kebab-case to camelCase
        /// </summary>
        /// <param name="input">The input string</param>
        /// <returns>The input string in camelCase</returns>
        public static string ToCamelCase(this string input)
        {
            if (string.IsNullOrEmpty(input) || input.Length < 2)
                return input;
            
            //If snake_case
            if (input.Contains('_') && char.IsLower(input[0]))
            {
                var parts = input.Split('_');
                return parts[0] + string.Join("", parts.Select(x => char.ToUpper(x[0]) + x[1..]));
            }

            //If PascalCase
            if (char.IsUpper(input[0]) && !input.Contains('_'))
                return char.ToLower(input[0]) + input[1..];

            //If kebab-case
            if (input.Contains('-') && char.IsLower(input[0]))
            {
                var parts = input.Split('-');
                return parts[0] + string.Join("", parts.Select(x => char.ToUpper(x[0]) + x[1..]));
            }

            return input;
        }

        /// <summary>
        /// Convert input string to snake_case
        /// </summary>
        /// <param name="input">The string to convert</param>
        /// <returns>The input string in snake_case</returns>
        public static string ToSnakeCase(this string input)
        {
            var sb = new StringBuilder();
            sb.Append(char.ToLower(input[0]));
            for(var i = 1; i < input.Length; ++i) {
                var c = input[i];
                if(char.IsUpper(c)) {
                    sb.Append('_');
                    sb.Append(char.ToLower(c));
                } else {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
    }
}