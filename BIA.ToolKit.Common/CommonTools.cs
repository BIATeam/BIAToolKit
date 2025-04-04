namespace BIA.ToolKit.Common
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    public static class CommonTools
    {
        public static List<string> BaseEntityInterfaces = new() { "IEntity<", "IEntityFixable<", "IEntityArchivable<" };
        
        /// <summary>
        /// Add a data to a dictionnary.
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="dico">The dictionnary to update</param>
        /// <param name="key">The data key</param>
        /// <param name="value">The data value</param>
        public static void AddToDictionnary<T1, T2>(Dictionary<T1, List<T2>> dico, T1 key, T2 value)
        {
            if (dico == null)
            {
                dico = new();
            }

            if (dico.ContainsKey(key))
            {
                dico[key].Add(value);
            }
            else
            {
                dico.Add(key, new List<T2> { value });
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
        /// Convert value from Kebab case to Pascal case.
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <returns>Value converted</returns>
        public static string ConvertKebabToPascalCase(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            string convertValue = "";
            value.Split('-').ToList().ForEach(p => convertValue += CultureInfo.CurrentCulture.TextInfo.ToTitleCase(p));
            return convertValue;
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
        /// Convert value to Pascal case.
        /// ex: planeAppService > PlaneAppService
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <returns>Value converted</returns>
        public static string ConvertToPascalCase(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length < 1)
                return value;
            return Char.ToUpperInvariant(value[0]) + value.Substring(1);
        }

        /// <summary>
        /// Verify if folder already exists and create it if not exists.
        /// </summary>
        /// <param name="folderPath">The folder path</param>
        public static void CheckFolder(string folderPath, bool deleteIfExist = false)
        {
            // Clean directory if wanted and already exist
            if (deleteIfExist && Directory.Exists(folderPath))
            {
                Directory.Delete(folderPath, true);
            }

            // Create directory if not exist
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
        }

        /// <summary>
        /// Generate a file with content and create parent folder if necessary.
        /// </summary>
        public static void GenerateFile(string fileName, List<string> fileLinesContent)
        {
            StringBuilder sb = new();
            fileLinesContent.ForEach(line => sb.AppendLine(line));

            // Verify if parent folder exist before create file
            DirectoryInfo parent = Directory.GetParent(fileName);
            CheckFolder(parent?.FullName);

            // Generate new file
            File.WriteAllText(fileName, sb.ToString());
        }

        /// <summary>
        /// Get first group that match with regex.
        /// </summary>
        public static string GetMatchRegexValue(string pattern, string data, int nbGoup = 1)
        {
            MatchCollection matches = new Regex(pattern).Matches(data);
            if (matches != null && matches.Count > 0)
            {
                GroupCollection groups = matches[0].Groups;
                if (groups.Count > nbGoup - 1)
                {
                    return groups[nbGoup].Value;
                }
            }
            return null;
        }

        /// <summary>
        /// Divide string data into 2 parts from char separator.
        /// </summary>
        public static (string, string) DivideDataFromSeparator(char separator, string data)
        {
            string regex = $@"^\s*(\w+)\s*{separator}\s*(\w+\W*\w*);\s*$";
            MatchCollection matches = new Regex(regex).Matches(data);
            if (matches != null && matches.Count > 0)
            {
                GroupCollection groups = matches[0].Groups;
                if (groups.Count > 2)
                {
                    // left part, right part
                    return (groups[1].Value, groups[2].Value);
                }
            }
            return default;
        }

        /// <summary>
        /// Check if data match with regex.
        /// </summary>
        public static bool IsMatchRegexValue(string pattern, string data)
        {
            MatchCollection matches = new Regex(pattern).Matches(data);
            return (matches != null && matches.Count > 0);
        }

        /// <summary>
        /// Deserialize Json file to object.
        /// </summary>
        public static T DeserializeJsonFile<T>(string fileName)
        {
            if (!File.Exists(fileName))
            {
                return default;
            }

            using StreamReader reader = new(fileName);
            string jsonString = reader.ReadToEnd();
            return JsonConvert.DeserializeObject<T>(jsonString);
        }

        /// <summary>
        /// Serialize object to Json file.
        /// </summary>
        public static void SerializeToJsonFile<T>(T jsonContent, string fileName)
        {
            string jsonString = JsonConvert.SerializeObject(jsonContent, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            File.WriteAllText(fileName, jsonString);
        }

        /// <summary>
        /// Verify if file contains occurence of datas.
        /// </summary>
        public static bool IsFileContainsData(List<string> fileLines, List<string> dataList)
        {
            foreach (string data in dataList)
            {
                if (fileLines.Where(line => line.Contains(data)).Any())
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Convert "string" element value to Enum element value.
        /// </summary>
        public static TEnum GetEnumElement<TEnum>(string element)
        {
            if (element != null)
            {
                if (Enum.TryParse(typeof(TEnum), element, out object result))
                {
                    return (TEnum)result;
                }
            }

            throw new Exception($"Element '{element}' not reconized for enum '{typeof(TEnum).Name}'.");
        }

        /// <summary>
        /// Get spaces at the beginning of the line.
        /// </summary>
        public static string GetSpacesBeginningLine(string line)
        {
            const string regex = @"^(\s*).+$";
            return GetMatchRegexValue(regex, line);
        }

        /// <summary>
        /// Get namespace path between "namespace" or "using" key.
        /// </summary>
        public static string GetNamespaceOrUsingValue(string line)
        {
            string regex = @"^\s*(?:namespace|using)\s([\w.]*);*\s*$";
            return GetMatchRegexValue(regex, line);
        }

        /// <summary>
        /// Check if line is a "namespace" or "using" line.
        /// </summary>
        public static bool IsNamespaceOrUsingLine(string line)
        {
            string regex = @"^\s*(?:namespace|using)\s(\w+)\.(\w+)\.";
            return IsMatchRegexValue(regex, line);
        }

        /// <summary>
        /// Return index of occurence number of value containing in a list.
        /// </summary>
        /// <param name="lines">List of lines</param>
        /// <param name="match">Value to found</param>
        /// <param name="occurence">Occurence number in the list</param>
        public static int IndexOfOccurence(List<string> lines, string match, int occurence)
        {
            int count = 0;
            int index = -1;

            var a = lines.Select(l => l.Contains(match, StringComparison.InvariantCultureIgnoreCase)).ToList();
            for (int i = 0; i < a.Count; i++)
            {
                if (a[i])
                {
                    index = i;
                    if (++count > occurence)
                    {
                        return index;
                    }
                }
            }

            return index;
        }

        /// <summary>
        /// Get enum corresponding value.
        /// </summary>
        public static T GetEnumValue<T>(string value)
        {
            return (T)Enum.Parse(typeof(T), value);
        }

        public static string GetBaseKeyType(List<string> baseList)
        {
            var iEntityBase = baseList.FirstOrDefault(x => BaseEntityInterfaces.Any(y => x.StartsWith(y)));
            if (iEntityBase == null)
                return null;

            var regex = new Regex(@"<\s*(\w+)\s*>");
            return regex.Match(iEntityBase).Groups[1].Value;
        }
    }
}
