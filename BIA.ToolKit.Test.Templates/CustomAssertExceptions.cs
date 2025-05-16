namespace BIA.ToolKit.Test.Templates
{
    using System;
    using static BIA.ToolKit.Application.Templates.Manifest.Feature;

    internal class FilesEqualsException(string details) : Exception($"files are not equals.\n{details}")
    { }

    internal class PartialInsertionMarkupNotFoundException(string partialInsertionMarkup, string filePath) : Exception($"\n-> Markup: \"{partialInsertionMarkup}\"\n-> File: {filePath}")
    { }
}
