namespace BIA.ToolKit.Test.Templates._5_0_0
{
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Test.Templates;
    using Xunit.Sdk;

    public sealed class GenerateTestFixture_5_0_0 : GenerateTestFixture
    {
        public GenerateTestFixture_5_0_0(IMessageSink diagnosticMessageSink) : base(diagnosticMessageSink)
        {
            Init("BIADemo_5.2.3", new Project
            {
                Name = "BIADemo",
                CompanyName = "TheBIADevCompany",
                BIAFronts = ["Angular"],
                FrameworkVersion = "5.2.3"
            });
        }
    }
}
