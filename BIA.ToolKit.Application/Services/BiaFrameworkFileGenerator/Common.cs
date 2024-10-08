namespace BIA.ToolKit.Application.Services.BiaFrameworkFileGenerator
{
    public static class Common
    {
        public const string TemplateKey_Mapper = "MapperTemplate.cshtml";
        public const string TemplateKey_Dto = "DtoTemplate.cshtml";
        public const string TemplateValue_BaseKeyType = "{BaseKeyType}";

        public static string ComputeNameArticle(string name)
        {
            const string An = "an";
            const string A = "a";

            var lowerName = name.ToLower();
            return
                lowerName.StartsWith("a") ||
                lowerName.StartsWith("e") ||
                lowerName.StartsWith("i") ||
                lowerName.StartsWith("o") ||
                lowerName.StartsWith("u") ?
                An : A;
        }
    }
}
