namespace BIA.ToolKit.Application.Templates
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    public static class Extensions
    {
        public static string ToCamelCase(this string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;

            if (s.All(c => !char.IsUpper(c) || c == s[0]))
            {
                return s.ToLowerInvariant();
            }

            return char.ToLowerInvariant(s[0]) + s.Substring(1);
        }

        public static string ToKebabCase(this string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;

            string result = Regex.Replace(s, @"(?<=[a-z0-9])([A-Z])", "-$1");
            return result.ToLowerInvariant();
        }

        public static string ToLitteral(this string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;

            string result = Regex.Replace(s, @"(?<=[a-z0-9])([A-Z])", " $1");
            return result;
        }
    }
}
