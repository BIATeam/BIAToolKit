namespace BIA.ToolKit.Domain.Work
{
    using System.Collections.Generic;

    public class CFOption
    {
        public int Default { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public bool IsChecked { get; set; }
        public IList<string> Files { get; set; }
        public IList<string> FilesToRemove { get; set; }
    }
}
