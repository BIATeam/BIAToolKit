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
    internal sealed class VersionedTemplateGenerator : TemplateGenerator
    {
        private string templateVersion;

        /// <summary>
        /// Set the template version (_X_Y_Z)
        /// </summary>
        /// <param name="templateVersion"></param>
        public void SetTemplateVersion(string templateVersion)
        {
            this.templateVersion = templateVersion;
        }

        protected override bool LoadIncludeText(string requestFileName, out string content, out string location)
        {
            if (requestFileName.EndsWith("ttinclude"))
            {
                var executingFolderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var searchingFolder = Path.Combine(executingFolderPath, templateVersion);
                if (!Directory.Exists(searchingFolder))
                    throw new Exception($"Unable to find template folder {searchingFolder}");

                var includeFileName = Path.GetFileName(requestFileName);
                var includeFile = Directory.GetFiles(searchingFolder, includeFileName, SearchOption.AllDirectories).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(includeFile))
                {
                    var includeContent = File.ReadAllText(includeFile);
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
