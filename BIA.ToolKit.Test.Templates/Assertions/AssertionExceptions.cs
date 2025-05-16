namespace BIA.ToolKit.Test.Templates.Assertions
{
    using System;
    using Microsoft.Build.Tasks;
    using System.Text;
    using static BIA.ToolKit.Application.Templates.Manifest.Feature;

    internal class AssertionException(string message) : Exception(message)
    { }

    internal class AllFilesEqualsException(string details) : AssertionException($"some files are not equals, see details below\n\n{details}")
    { }

    internal class FilesEqualsException(string referencePath, string generatedPath, int modifiedLines, int movedLines, int addedLines, int deletedLines) : AssertionException(GenerateErrorDetails(referencePath, generatedPath, modifiedLines, movedLines, addedLines, deletedLines))
    {
        private static string GenerateErrorDetails(string referencePath, string generatedPath, int modifiedLines, int movedLines, int addedLines, int deletedLines)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"File: {Path.GetFileName(generatedPath)}");
            builder.AppendLine($"Diff: {modifiedLines} lines modified, {movedLines} lines moved, {addedLines} lines added, {deletedLines} lines deleted");
            builder.Append($"kdiff3.exe {referencePath} {generatedPath}");
            return builder.ToString();
        }
    }

    internal class PartialInsertionMarkupNotFoundException(string partialInsertionMarkup, string filePath) : AssertionException($"File: {filePath}\nMarkup: \"{partialInsertionMarkup}\"")
    { }
    
    internal class ReferenceFileNotFoundException(string filePath) : AssertionException($"File: {filePath}")
    { }

    internal class GeneratedFileNotFoundException(string filePath) : AssertionException($"File: {filePath}")
    { }
}
