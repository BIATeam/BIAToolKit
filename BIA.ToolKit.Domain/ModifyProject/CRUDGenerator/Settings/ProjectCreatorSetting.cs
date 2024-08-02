namespace BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class ProjectCreatorSetting
    {
        public WithoutAppFeature WithoutAppFeature { get; set; } = new WithoutAppFeature();
    }

    public class WithoutAppFeature
    {
        public List<string> FilesToExcludes { get; set; } = new List<string>();
    }


}
