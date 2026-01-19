namespace BIA.ToolKit.Domain.ProjectAnalysis
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public sealed record ClassInfo(
        string ClassName,
        string FilePath,
        string Namespace,
        bool IsGeneric,
        IReadOnlyList<AttributeInfo> Attributes,
        IReadOnlyList<PropertyInfo> PublicProperties,      // Dédupliquées et héritées incluses
        IReadOnlyList<InheritedTypeInfo> BaseClassesChain, // De la plus proche à System.Object (exclue)
        IReadOnlyList<InheritedTypeInfo> AllInterfaces     // Transitivement
    );
}
