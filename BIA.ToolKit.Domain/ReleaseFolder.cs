﻿namespace BIA.ToolKit.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using BIA.ToolKit.Domain.Settings;

    public class ReleaseFolder(string name, string originPath, string repositoryName) : Release(name, originPath, repositoryName)
    {
        public override ReleaseType ReleaseType => ReleaseType.Folder;

        public override async Task DownloadAsync()
        {
            InitDownload();

            await Task.Run(() =>
            {
                foreach (var source in Directory.GetFiles(OriginPath, "*", SearchOption.AllDirectories))
                {
                    var dest = source.Replace(OriginPath, LocalPath);
                    Directory.CreateDirectory(Path.GetDirectoryName(dest));
                    File.Copy(source, dest, true);
                }
            });

            IsDownloaded = true;
        }

        protected override void InitDownload()
        {
            if (!Directory.Exists(OriginPath))
            {
                throw new DirectoryNotFoundException(OriginPath);
            }

            base.InitDownload();
        }
    }
}
