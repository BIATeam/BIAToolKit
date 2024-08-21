namespace BIA.ToolKit.Domain.Model
{
    public class FeatureSetting
    {
        public bool WithFrontEnd { get; set; } = true;

        public bool WithFrontFeature { get; set; } = true;

        public bool WithServiceApi { get; set; } = true;

        public bool WithDeployDb { get; set; } = true;

        public bool WithWorkerService { get; set; } = true;

        public bool HasAllFeature =>
            WithFrontEnd &&
            WithFrontFeature &&
            WithServiceApi &&
            WithDeployDb &&
            WithWorkerService;
    }
}
