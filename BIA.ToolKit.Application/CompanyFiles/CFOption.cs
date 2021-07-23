namespace BIA.ToolKit.Application.CompanyFiles
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class CFOption
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public bool IsChecked { get; set; }
        public IList<string> Files { get; set; }
        public IList<string> FilesToRemove { get; set; }
    }
}
