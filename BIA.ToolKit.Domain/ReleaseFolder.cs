namespace BIA.ToolKit.Domain
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
    }
}
