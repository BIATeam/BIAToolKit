namespace BIA.ToolKit.Application.CompanyFiles
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class CFSettings
    {
        public IList<string> Profiles { get; set; }
        public IList<CFOption> Options { get; set; }
    }
}
