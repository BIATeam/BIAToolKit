namespace BIA.ToolKit.Test.Templates._7_0_0
{
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Test.Templates;

    public sealed class GenerateTestFixture_7_0_0 : GenerateTestFixture
    {
        public GenerateTestFixture_7_0_0()
        {
            Init("BIADemo_7.0.0", new Project
            {
                Name = "BIADemo",
                CompanyName = "TheBIADevCompany",
                BIAFronts = ["Angular"],
                FrameworkVersion = "7.0.0"
            });
        }
    }
}
