namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Domain.Model;

    public class FeatureSettingVM : ObservableObject
    {
        public FeatureSetting Model { get; private set; }

        public FeatureSettingVM()
        {
            Model = new FeatureSetting();
        }

        public FeatureSettingVM(FeatureSetting model)
        {
            this.Model = model;
        }

        public bool WithFrontEnd
        {
            get { return Model.WithFrontEnd; }
            set
            {
                if (Model.WithFrontEnd != value)
                {
                    Model.WithFrontEnd = value;
                    RaisePropertyChanged(nameof(WithFrontEnd));
                }
            }
        }

        public bool WithFrontFeature
        {
            get { return Model.WithFrontFeature; }
            set
            {
                if (Model.WithFrontFeature != value)
                {
                    Model.WithFrontFeature = value;
                    RaisePropertyChanged(nameof(WithFrontFeature));

                    if (WithFrontFeature && !WithDeployDb)
                    {
                        WithDeployDb = true;
                    }
                }
            }
        }

        public bool WithServiceApi
        {
            get { return Model.WithServiceApi; }
            set
            {
                if (Model.WithServiceApi != value)
                {
                    Model.WithServiceApi = value;
                    RaisePropertyChanged(nameof(WithServiceApi));
                }
            }
        }

        public bool WithDeployDb
        {
            get { return Model.WithDeployDb; }
            set
            {
                if (Model.WithDeployDb != value)
                {
                    Model.WithDeployDb = value;
                    RaisePropertyChanged(nameof(WithDeployDb));
                }
            }
        }

        public bool WithWorkerService
        {
            get { return Model.WithWorkerService; }
            set
            {
                if (Model.WithWorkerService != value)
                {
                    Model.WithWorkerService = value;
                    RaisePropertyChanged(nameof(WithWorkerService));

                    if(WithWorkerService && !WithDeployDb)
                    {
                        WithDeployDb = true;
                    }
                }
            }
        }
    }
}
