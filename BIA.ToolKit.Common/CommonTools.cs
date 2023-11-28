namespace BIA.ToolKit.Common
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;

    public static class CommonTools
    {
        /// <summary>
        /// Add a data to a dictionnary.
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="dico">The dictionnary to update</param>
        /// <param name="key">The data key</param>
        /// <param name="value">The data value</param>
        public static void AddToDictionnary<T>(Dictionary<string, List<T>> dico, string key, T value)
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

        /// <summary>
        /// Convert value from Pascal case to Kebab case.
        /// ex: PlaneAppService > plane-app-service
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <returns>Value converted</returns>
        public static string ConvertPascalToKebabCase(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return Regex.Replace(value, "(?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z0-9])", "-$1", RegexOptions.Compiled)
                .Trim().ToLower();
        }

        /// <summary>
        /// Convert value to Camel case.
        /// ex: PlaneAppService > planeAppService
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <returns>Value converted</returns>
        public static string ConvertToCamelCase(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length < 1)
                return value;
            return Char.ToLowerInvariant(value[0]) + value.Substring(1);
        }

        /// <summary>
        /// Delete folder if already exists and create it after.
        /// </summary>
        /// <param name="folderPath">The folder path</param>
        public static void PrepareFolder(string folderPath)
        {
            // Clean destination directory if already exist
            if (Directory.Exists(folderPath))
            {
                Directory.Delete(folderPath, true);
            }
            Directory.CreateDirectory(folderPath);
        }

        /// <summary>
        /// Verify if folder already exists and create it if not exists.
        /// </summary>
        /// <param name="folderPath">The folder path</param>
        public static void CheckFolder(string folderPath)
        {
            // Create directory if not exist
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
        }

    }
}
