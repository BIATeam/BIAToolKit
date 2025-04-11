namespace BIA.ToolKit.Test.Templates
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal class FilesEqualsException(string details) : Exception($"files are not equals.\n{details}")
    {
    }
}
