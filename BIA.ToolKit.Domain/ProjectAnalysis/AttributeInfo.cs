namespace BIA.ToolKit.Domain.ProjectAnalysis
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public sealed record AttributeInfo(
        string Name,
        IReadOnlyDictionary<string, string> Arguments // ctor + named
    );
}
