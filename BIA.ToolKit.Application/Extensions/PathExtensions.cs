namespace BIA.ToolKit.Application.Extensions
{ 
    public static class PathExtensions
    {
        public static string NormalizePath(this string path)
        {
            return path.Replace('\\', '/');
        }
    }
}