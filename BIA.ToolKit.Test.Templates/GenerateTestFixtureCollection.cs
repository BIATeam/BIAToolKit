namespace BIA.ToolKit.Test.Templates
{
    using BIA.ToolKit.Test.Templates._5_0_0;
    using BIA.ToolKit.Test.Templates._6_0_0;

    [CollectionDefinition(nameof(GenerateTestFixtureCollection))]
    public class GenerateTestFixtureCollection :
        ICollectionFixture<GenerateTestFixture_5_0_0>,
        ICollectionFixture<GenerateTestFixture_6_0_0>
    {
    }
}
