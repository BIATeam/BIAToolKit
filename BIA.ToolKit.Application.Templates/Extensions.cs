namespace BIA.ToolKit.Application.Templates
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Humanizer;

    public static class Extensions
    {
        public static string ToCamelCase(this string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;

            return char.ToLowerInvariant(s[0]) + s.Substring(1);
        }

        public static string ToKebabCase(this string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;

            string result = Regex.Replace(s, @"(?<=[a-z0-9])([A-Z])", "-$1");
            return result.ToLowerInvariant();
        }

        /// <summary>
        /// Converts CamelCase to literal. 
        /// For exemple transform "OkItIsTimeToStopBPANow" in "ok it is time to stop BPA now"
        /// </summary>
        /// <param name="s">The s.</param>
        /// <returns></returns>
        public static string ToLiteral(this string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;

            string result = Regex.Replace(s, @"\p{Ll}\p{Lu}", m => m.Value[0] + " " + m.Value[1]);
            result = Regex.Replace(result, @"\p{Lu}\p{Lu}\p{Ll}", m => m.Value[0] + " " + m.Value[1] + m.Value[2]);
            result = Regex.Replace(result, @"\p{Lu}\p{Ll}", m => "" + char.ToLowerInvariant(m.Value[0]) + m.Value[1]);
            return result;
        }

        public static string ToSingular(this string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;

            string result = s.Singularize();
            return result;
        }
    }
}
