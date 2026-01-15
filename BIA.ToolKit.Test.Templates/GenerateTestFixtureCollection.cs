namespace BIA.ToolKit.Test.Templates
{
    using BIA.ToolKit.Test.Templates._5_0_0;
    using BIA.ToolKit.Test.Templates._6_0_0;

    [CollectionDefinition(nameof(GenerateTestFixtureCollection_5_0_0))]
    public class GenerateTestFixtureCollection_5_0_0 :
        ICollectionFixture<GenerateTestFixture_5_0_0>
    {
    }

    [CollectionDefinition(nameof(GenerateTestFixtureCollection_6_0_0))]
    public class GenerateTestFixtureCollection_6_0_0 :
        ICollectionFixture<GenerateTestFixture_6_0_0>
    {
    }
}
