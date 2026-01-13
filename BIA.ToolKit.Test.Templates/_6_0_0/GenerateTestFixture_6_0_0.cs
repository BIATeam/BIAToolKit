namespace BIA.ToolKit.Test.Templates._6_0_0
{
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Test.Templates;

    public sealed class GenerateTestFixture_6_0_0 : GenerateTestFixture
    {
        public GenerateTestFixture_6_0_0()
        {
            Init("BIADemo_6.0.1", new Project
            {
                Name = "BIADemo",
                CompanyName = "TheBIADevCompany",
                BIAFronts = ["Angular"],
                FrameworkVersion = "6.0.1"
            });
        }
    }
}
