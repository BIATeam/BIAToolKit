namespace BIA.ToolKit.Application.Services.FileGenerator
{
    public static class Common
    {
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
