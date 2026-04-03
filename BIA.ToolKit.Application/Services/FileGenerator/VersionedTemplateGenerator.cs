namespace BIA.ToolKit.Application.Services.FileGenerator
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using Mono.TextTemplating;

    /// <summary>
    /// Extended class of <see cref="TemplateGenerator"/> used for versionned templates.
    /// </summary>
    /// <param name="templateVersion">Template version at format _X_Y_Z</param>
    internal sealed class VersionedTemplateGenerator(string templateVersion) : TemplateGenerator
    {
        protected override bool LoadIncludeText(string requestFileName, out string content, out string location)
        {
            if (requestFileName.EndsWith("ttinclude"))
            {
                string executingFolderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string searchingFolder = Path.Combine(executingFolderPath, templateVersion);
                if (!Directory.Exists(searchingFolder))
                    throw new Exception($"Unable to find template folder {searchingFolder}");

                string includeFileName = Path.GetFileName(requestFileName);
                string includeFile = Directory.GetFiles(searchingFolder, includeFileName, SearchOption.AllDirectories).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(includeFile))
                {
                    string includeContent = File.ReadAllText(includeFile);
                    // Replace the load of assembly based on $(TargetPath) from template content to avoid generation errors
                    includeContent = includeContent.Replace("<#@ assembly name=\"$(TargetPath)\" #>", "<#@ #>");
                    content = includeContent;
                    location = requestFileName;
                    return true;
                }
            }

            return base.LoadIncludeText(requestFileName, out content, out location);
        }
    }
}
