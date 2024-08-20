namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;

    public class FeatureViewModel : ObservableObject
    {
        private bool withFrontEnd { get; set; } = true;

        private bool withFrontFeature { get; set; } = true;

        private bool withServiceApi { get; set; } = true;

        private bool withDeployDb { get; set; } = true;

        private bool withWorkerService { get; set; } = true;

        private bool withInfraData { get; set; } = true;

        public bool WithFrontEnd
        {
            get { return withFrontEnd; }
            set
            {
                if (withFrontEnd != value)
                {
                    withFrontEnd = value;
                    RaisePropertyChanged(nameof(WithFrontEnd));
                }
            }
        }

        public bool WithFrontFeature
        {
            get { return withFrontFeature; }
            set
            {
                if (withFrontFeature != value)
                {
                    withFrontFeature = value;
                    RaisePropertyChanged(nameof(WithFrontFeature));
                }
            }
        }

        public bool WithServiceApi
        {
            get { return withServiceApi; }
            set
            {
                if (withServiceApi != value)
                {
                    withServiceApi = value;
                    RaisePropertyChanged(nameof(WithServiceApi));
                }
            }
        }

        public bool WithDeployDb
        {
            get { return withDeployDb; }
            set
            {
                bool newValue = value;

                if (withFrontFeature)
                {
                    newValue = true;
                }
                else if (!withWorkerService)
                {
                    newValue = false;
                }

                if (withDeployDb != newValue)
                {
                    withDeployDb = newValue;
                    RaisePropertyChanged(nameof(WithDeployDb));
                }
            }
        }

        public bool WithWorkerService
        {
            get { return withWorkerService; }
            set
            {
                if (withWorkerService != value)
                {
                    withWorkerService = value;
                    RaisePropertyChanged(nameof(WithWorkerService));
                }
            }
        }

        public bool WithInfraData
        {
            get { return withInfraData; }
            set
            {
                if (withInfraData != value)
                {
                    withInfraData = value;
                    RaisePropertyChanged(nameof(WithInfraData));
                }
            }
        }
    }
}
