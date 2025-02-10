namespace BIA.ToolKit.Package
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    public static class Helper
    {
        public static PackageConfig GetPackageConfig(string currentApplicationPath)
        {
            string packageJsonPath = Path.Combine(currentApplicationPath, "package.json");

            if (!File.Exists(packageJsonPath))
            {
                throw new FileNotFoundException("package.json not found.");
            }

            string jsonContent = File.ReadAllText(packageJsonPath);
            var packageConfig = JsonConvert.DeserializeObject<PackageConfig>(jsonContent);

            if (packageConfig == null || string.IsNullOrEmpty(packageConfig.DistributionServer)
                || string.IsNullOrEmpty(packageConfig.PackageVersionFileName)
                || string.IsNullOrEmpty(packageConfig.PackageArchiveName))
            {
                throw new InvalidOperationException("Missing required values in package.json.");
            }

            return packageConfig;
        }
    }
}
