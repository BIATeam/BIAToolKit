namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Domain.Main;

    public class MainViewModel : ObservableObject
    {
        public MainViewModel()
        {
            Main = new Main();
        }

        //public IProductRepository Repository { get; set; }
        public Main Main { get; set; }

        public string RootProjectsPath
        {
            get { return Main.RootProjectsPath; }
            set
            {
                if (Main.RootProjectsPath != value)
                {
                    Main.RootProjectsPath = value;
                    RaisePropertyChanged("RootProjectsPath");
                }
            }
        }
    }
}
