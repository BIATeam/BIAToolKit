namespace BIA.ToolKit.Common
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    public static class CommonTools
    {
        public static void AddToDictionnary<T>(Dictionary<T, List<T>> dico, T key, T value)
        {
            if (dico.ContainsKey(key))
            {
                dico[key].Add(value);
            }
            else
            {
                dico.Add(key, new List<T> { value });
            }
        }

        public static string ConvertPascalToKebabCase(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return Regex.Replace(value, "(?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z0-9])", "-$1", RegexOptions.Compiled)
                .Trim().ToLower();
        }

        public static string ConvertToCamelCase(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length < 1)
                return value;
            return Char.ToLowerInvariant(value[0]) + value.Substring(1);
        }

    }
}
