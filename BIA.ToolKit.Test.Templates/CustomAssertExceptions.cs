namespace BIA.ToolKit.Test.Templates
{
    using System;

    internal class FilesEqualsException(string details) : Exception($"files are not equals.\n{details}")
    {
    }
}
