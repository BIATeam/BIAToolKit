using System.Linq;

namespace BIA.ToolKit.Domain.ModifyProject.DtoGenerator
{
    public class ProjectInfo(string baseDirectory, string fullName, TemplateType templateType, UiFramework uiFramework, bool tiered)
    {
        public string BaseDirectory { get; } = baseDirectory;
        public string FullName { get; } = fullName;
        public string Name => FullName.Split('.').Last();
        public TemplateType TemplateType { get; } = templateType;
        public UiFramework UiFramework { get; } = uiFramework;
        public bool Tiered { get; } = tiered;

        public override string ToString()
        {
            return $"{nameof(BaseDirectory)}: {BaseDirectory}, {nameof(FullName)}: {FullName}, {nameof(Name)}: {Name}, {nameof(TemplateType)}: {TemplateType}, {nameof(UiFramework)}: {UiFramework}, {nameof(Tiered)}: {Tiered}";
        }
    }

    public enum TemplateType
    {
        Application,
        Module
    }

    public enum UiFramework
    {
        None,
        RazorPages,
        Angular
    }
}
