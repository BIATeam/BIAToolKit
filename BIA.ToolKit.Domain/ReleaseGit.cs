namespace BIA.ToolKit.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Octokit;

    public class ReleaseGit(string name, string originPath, string repositoryName, ReleaseType releaseType, IReadOnlyList<ReleaseAsset> assets) : Release(name, originPath, repositoryName)
    {
        private readonly ReleaseType releaseType = releaseType;
        public override ReleaseType ReleaseType => releaseType;

        public IReadOnlyList<ReleaseAsset> Assets { get; init; } = assets;
    }
}
