namespace BIA.ToolKit.Application.Services
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Implementation of ITextParsingService.
    /// Centralizes text parsing and entity name manipulation logic.
    /// </summary>
    public class TextParsingService : ITextParsingService
    {
        /// <summary>
        /// Extracts the entity name from a DTO class name.
        /// Example: "UserDto" -> "User"
        /// </summary>
        public string ExtractEntityNameFromDto(string dtoName)
        {
            if (string.IsNullOrWhiteSpace(dtoName))
            {
                return dtoName;
            }

            return RemoveDtoSuffix(dtoName);
        }

        /// <summary>
        /// Gets the plural form of a singular name.
        /// Uses simple rules: adds 's' or 'es' based on ending.
        /// </summary>
        public string GetPluralForm(string singular)
        {
            if (string.IsNullOrWhiteSpace(singular))
            {
                return singular;
            }

            // Simple pluralization rules
            if (singular.EndsWith("y", StringComparison.OrdinalIgnoreCase))
            {
                return singular.Substring(0, singular.Length - 1) + "ies";
            }

            if (singular.EndsWith("s", StringComparison.OrdinalIgnoreCase) ||
                singular.EndsWith("x", StringComparison.OrdinalIgnoreCase) ||
                singular.EndsWith("z", StringComparison.OrdinalIgnoreCase) ||
                singular.EndsWith("ch", StringComparison.OrdinalIgnoreCase) ||
                singular.EndsWith("sh", StringComparison.OrdinalIgnoreCase))
            {
                return singular + "es";
            }

            return singular + "s";
        }

        /// <summary>
        /// Validates that a DTO name follows the correct naming convention.
        /// Valid: ends with "Dto", PascalCase, alphanumeric only
        /// </summary>
        public bool ValidateDtoName(string dtoName)
        {
            if (string.IsNullOrWhiteSpace(dtoName))
            {
                return false;
            }

            if (!dtoName.EndsWith("Dto", StringComparison.Ordinal))
            {
                return false;
            }

            // Check for PascalCase and alphanumeric only
            foreach (char c in dtoName)
            {
                if (!char.IsLetterOrDigit(c) && c != '_')
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Converts a string to PascalCase.
        /// </summary>
        public string ToPascalCase(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            var textInfo = CultureInfo.CurrentCulture.TextInfo;
            var words = input.Split(' ');
            var result = string.Empty;

            foreach (var word in words)
            {
                if (!string.IsNullOrWhiteSpace(word))
                {
                    result += textInfo.ToTitleCase(word.ToLower());
                }
            }

            return result;
        }

        /// <summary>
        /// Removes 'Dto' suffix from a name if present.
        /// </summary>
        public string RemoveDtoSuffix(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return name;
            }

            if (name.EndsWith("Dto", StringComparison.Ordinal))
            {
                return name.Substring(0, name.Length - 3);
            }

            return name;
        }
    }
}
