﻿namespace BIA.ToolKit.Domain.Extensions
{ 
    public static class PathExtensions
    {
        public static string NormalizePath(this string path)
        {
            return path.Replace('\\', '/');
        }
    }
}