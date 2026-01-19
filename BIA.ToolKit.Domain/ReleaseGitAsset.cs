namespace BIA.ToolKit.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public sealed class ReleaseGitAsset(string name, string downloadUrl, long? size = null)
    {
        public string Name { get; } = name;
        public string DownloadUrl { get; } = downloadUrl;
        public long Size { get; } = size.GetValueOrDefault();
        public bool HasSize => size.HasValue;
    }
}
