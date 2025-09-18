namespace BIA.ToolKit.Application.Helper
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using BIA.ToolKit.Domain.ModifyProject;

    public class NamesAndVersionResolver
    {
        // RegExp for path to find a file that contain FrameworkVersion. Param1 in Path is the Company, Param2 in Path is the ProjectName
        public string ConstantFileRegExpPath { get; set; }
        // Pattern corresponding to the name of the file that contain FrameworkVersion.
        public string ConstantFileNameSearchPattern { get; set; }
        // RegExp to determine de Framewprkversion in the file: It is the Param 1
        public string ConstantFileRegExpVersion { get; set; }
        // RegExp to determine the namespace of the constant file. Param1 in Path is the Company, Param2 in Path is the ProjectName
        public string ConstantFileNamespace { get; set; }

        // RegExp for path of an unique file in Front 
        public string[] FrontFileRegExpPath { get; set; }
        // RegExp for path of the package.json file
        public string FrontFileUsingBiaNg { get; set; }
        // RegExp for findind import of bia-ng in package.json file
        public string FrontFileBiaNgImportRegExp { get; set; }
        // Pattern corresponding to the name of the file unique file in Front.
        public string FrontFileNameSearchPattern { get; set; }

        public void ResolveNamesAndVersion(Project currentProject)
        {
            if (!string.IsNullOrEmpty(ConstantFileNameSearchPattern))
            {
                var reg = new Regex(currentProject.Folder.Replace("\\", "\\\\") + ConstantFileRegExpPath, RegexOptions.IgnoreCase);
                string file = currentProject.ProjectFiles.Where(path => path.EndsWith(ConstantFileNameSearchPattern) && reg.IsMatch(path))?.FirstOrDefault();
                if (file != null)
                {
                    var regProjectName = new Regex(ConstantFileNamespace);
                    var regVersion = new Regex(ConstantFileRegExpVersion);

                    foreach (var line in File.ReadAllLines(file))
                    {
                        var matchNamespace = regProjectName.Match(line);
                        if (matchNamespace.Success)
                        {
                            currentProject.CompanyName = matchNamespace.Groups[1].Value;
                            currentProject.Name = matchNamespace.Groups[2].Value;
                            continue;
                        }

                        var matchVersion = regVersion.Match(line);
                        if (matchVersion.Success)
                        {
                            currentProject.FrameworkVersion = matchVersion.Groups[1].Value;
                            break;
                        }
                    }
                }
            }
            if (!string.IsNullOrEmpty(FrontFileNameSearchPattern))
            {
                currentProject.BIAFronts.Clear();
                var regPackageJson = new Regex(currentProject.Folder.Replace("\\", "\\\\") + FrontFileUsingBiaNg, RegexOptions.IgnoreCase);
                List<string> packagesJsonFiles = currentProject.ProjectFiles.Where(path => path.EndsWith("package.json") && regPackageJson.IsMatch(path)).ToList();
                if (packagesJsonFiles != null)
                {
                    foreach (var fileFront in packagesJsonFiles)
                    {
                        string fileContent = File.ReadAllText(fileFront);
                        if (fileContent.Contains(FrontFileBiaNgImportRegExp))
                        {
                            var match = regPackageJson.Match(fileFront);
                            if (!currentProject.BIAFronts.Contains(match.Groups[1].Value))
                            {
                                currentProject.BIAFronts.Add(match.Groups[1].Value);
                            }
                        }
                    }
                }


                foreach (string regex in FrontFileRegExpPath)
                {
                    var reg2 = new Regex(currentProject.Folder.Replace("\\", "\\\\") + regex, RegexOptions.IgnoreCase);
                    List<string> filesFront = currentProject.ProjectFiles.Where(path => path.EndsWith(FrontFileNameSearchPattern) && reg2.IsMatch(path)).ToList();

                    if (filesFront != null)
                    {
                        foreach (var fileFront in filesFront)
                        {
                            var match = reg2.Match(fileFront);
                            if (!currentProject.BIAFronts.Contains(match.Groups[1].Value))
                            {
                                currentProject.BIAFronts.Add(match.Groups[1].Value);
                            }
                        }
                    }
                }

            }
        }
    }
}
