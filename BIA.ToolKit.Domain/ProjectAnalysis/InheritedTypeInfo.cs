namespace BIA.ToolKit.Domain.ProjectAnalysis
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public sealed record InheritedTypeInfo(
        string DisplayName,       // Avec génériques
        bool HasGenerics,
        IReadOnlyList<string> GenericArguments // Format affichable
    );
}
