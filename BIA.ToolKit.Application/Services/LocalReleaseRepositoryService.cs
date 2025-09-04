namespace BIA.ToolKit.Application.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class LocalReleaseRepositoryService
    {
        const string BiaTemplateReleasesFolderName = "BiaTemplate";
        const string BiaToolkitReleasesFolderName = "BiaToolkit";
        const string BiaToolkitReleaseZipName = "BiaToolkit.zip";
        const string BiaToolkitUpdaterReleaseZipName = "BiaToolkitUpdater.zip";

        private string localReleaseRepositoryFolder;
        public bool UseLocalReleaseRepository { get; private set; }

        public void Init(bool useLocalReleaseRepository, string localReleaseRepositoryFolder)
        {
            this.UseLocalReleaseRepository = useLocalReleaseRepository;
            this.localReleaseRepositoryFolder = localReleaseRepositoryFolder;
        }

        public string GetBiaTemplateReleaseArchivePath(string version)
        {
            return Path.Combine(this.localReleaseRepositoryFolder, BiaTemplateReleasesFolderName, $"{version}.zip");
        }

        public string GetBiaToolkitUpdaterReleaseArchivePath(string version)
        {
            return Path.Combine(this.localReleaseRepositoryFolder, BiaToolkitReleasesFolderName, version, BiaToolkitUpdaterReleaseZipName);
        }

        public string GetBiaToolkitReleaseArchivePath(string version)
        {
            return Path.Combine(this.localReleaseRepositoryFolder, BiaToolkitReleasesFolderName, version, BiaToolkitReleaseZipName);
        }
    }
}
