namespace BIA.ToolKit.Application.Services
{
    /// <summary>
    /// Service interface for text parsing and entity name manipulation.
    /// Centralizes text parsing logic used across multiple components.
    /// </summary>
    public interface ITextParsingService
    {
        /// <summary>
        /// Extracts the entity name from a DTO class name.
        /// Example: "UserDto" -> "User"
        /// </summary>
        /// <param name="dtoName">The DTO class name.</param>
        /// <returns>The extracted entity name.</returns>
        string ExtractEntityNameFromDto(string dtoName);

        /// <summary>
        /// Gets the plural form of a singular name.
        /// </summary>
        /// <param name="singular">The singular form.</param>
        /// <returns>The plural form.</returns>
        string GetPluralForm(string singular);

        /// <summary>
        /// Validates that a DTO name follows the correct naming convention.
        /// </summary>
        /// <param name="dtoName">The DTO name to validate.</param>
        /// <returns>True if valid, false otherwise.</returns>
        bool ValidateDtoName(string dtoName);

        /// <summary>
        /// Converts a string to PascalCase.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>The PascalCase string.</returns>
        string ToPascalCase(string input);

        /// <summary>
        /// Removes 'Dto' suffix from a name if present.
        /// </summary>
        /// <param name="name">The name to process.</param>
        /// <returns>The name without 'Dto' suffix.</returns>
        string RemoveDtoSuffix(string name);

        /// <summary>
        /// Extracts the class name from a file path (without extension).
        /// Example: "C:\\Project\\MyClass.cs" -> "MyClass".
        /// </summary>
        /// <param name="filePath">The file path or file name.</param>
        /// <returns>The class name without extension.</returns>
        string ExtractClassNameFromFile(string filePath);

        /// <summary>
        /// Extracts the entity name from a DTO file path.
        /// Handles both file names and full paths.
        /// Example: "C:\Project\UserDto.cs" -> "User"
        /// </summary>
        /// <param name="dtoFilePath">The DTO file path or name.</param>
        /// <returns>The extracted entity name.</returns>
        string ExtractEntityNameFromDtoFile(string dtoFilePath);
    }
}
