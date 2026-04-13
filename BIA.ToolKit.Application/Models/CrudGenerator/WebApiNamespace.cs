namespace BIA.ToolKit.Application.Models.CrudGenerator
{
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator;

    public class WebApiNamespace(WebApiFileType fileType, string crudNamespace)
    {
        public WebApiFileType FileType { get; } = fileType;

        public string CrudNamespace { get; } = crudNamespace;

        public string CrudNamespaceGenerated { get; set; }
    }
}
