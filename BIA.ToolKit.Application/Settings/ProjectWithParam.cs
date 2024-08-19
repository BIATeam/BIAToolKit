namespace BIA.ToolKit.Application.Settings
{
    public class ProjectWithParam
    {
        public bool WithFrontEnd { get; set; }

        public bool WithFrontFeature { get; set; }

        public bool WithServiceApi { get; set; }

        public bool WithDeployDb { get; set; }

        public bool WithWorkerService { get; set; }

        public bool WithHangfire { get; set; }

        public bool WithInfraData { get; set; }

        public bool HasAllFeature =>
            WithFrontEnd &&
            WithFrontFeature &&
            WithServiceApi &&
            WithDeployDb &&
            WithWorkerService &&
            WithHangfire &&
            WithInfraData;
    }
}
