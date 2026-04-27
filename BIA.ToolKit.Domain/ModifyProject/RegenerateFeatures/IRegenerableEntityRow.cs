namespace BIA.ToolKit.Domain.ModifyProject.RegenerateFeatures;

public interface IRegenerableEntityRow
{
    RegenerableEntity Entity { get; }
    string EntityNameSingular { get; }
    bool IsCrudEnabled { get; }
    bool IsOptionEnabled { get; }
    bool IsCrudSelected { get; set; }
    bool IsOptionSelected { get; set; }
    bool IsDtoSelected { get; set; }
}
