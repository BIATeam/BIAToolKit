namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Domain.Main;

    public class MainViewModel : ObservableObject
    {
        public MainViewModel()
        {
            Main = new Main();
            SynchronizeSettings.AddCallBack("RootProjectsPath", DelegateSetRootProjectsPath);
        }

        //public IProductRepository Repository { get; set; }
        public Main Main { get; set; }

        public void DelegateSetRootProjectsPath(string value)
        {
            Main.RootProjectsPath = value;
            RaisePropertyChanged("RootProjectsPath");
        }

        public string RootProjectsPath
        {
            get { return Main.RootProjectsPath; }
            set
            {
                if (Main.RootProjectsPath != value)
                {
                    SynchronizeSettings.SettingChange("RootProjectsPath", value);
                    /*Main.RootProjectsPath = value;
                    RaisePropertyChanged("RootProjectsPath");*/
                }
            }
        }
    }
}
