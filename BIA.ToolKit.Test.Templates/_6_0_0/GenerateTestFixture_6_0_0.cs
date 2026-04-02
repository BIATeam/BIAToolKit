namespace BIA.ToolKit.Test.Templates._6_0_0
{
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Test.Templates;
    using Xunit.Sdk;

    public sealed class GenerateTestFixture_6_0_0 : GenerateTestFixture
    {
        public GenerateTestFixture_6_0_0(IMessageSink diagnosticMessageSink) : base(diagnosticMessageSink)
        {
            Init("BIADemo_6.1.2", new Project
            {
                Name = "BIADemo",
                CompanyName = "TheBIADevCompany",
                BIAFronts = ["Angular"],
                FrameworkVersion = "6.1.2"
            });
        }
    }
}
