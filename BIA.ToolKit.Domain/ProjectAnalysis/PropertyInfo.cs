namespace BIA.ToolKit.Domain.ProjectAnalysis
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public sealed record PropertyInfo(
        string TypeName,
        string Name,
        bool IsExplicitInterfaceImplementation,
        IReadOnlyList<AttributeInfo> Attributes
    );
}
