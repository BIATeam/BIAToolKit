namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;

    public class IncludeProjectViewModel : ObservableObject
    {
        private bool withFrontEnd = true;
        private bool withDatabase = true;

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

        public bool WithDatabase
        {
            get { return withDatabase; }
            set
            {
                if (withDatabase != value)
                {
                    withDatabase = value;
                    RaisePropertyChanged(nameof(WithDatabase));
                }
            }
        }
    }

}
