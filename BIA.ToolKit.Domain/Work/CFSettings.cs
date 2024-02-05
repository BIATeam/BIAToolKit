namespace BIA.ToolKit.Domain.Work
{
    using System.Collections.Generic;

    public class CFSettings
    {
        public IList<string>? Profiles { get; set; }
        public IList<CFOption>? Options { get; set; }
    }
}
