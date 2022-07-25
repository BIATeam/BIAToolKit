namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Domain.ModifyProject;
    using System;

    public class ModifyProjectViewModel : ObservableObject
    {
        public ModifyProjectViewModel()
        {
            ModifyProject = new ModifyProject();
        }

        //public IProductRepository Repository { get; set; }
        public ModifyProject ModifyProject { get; set; }

        public string Folder
        {
            get { return ModifyProject.CurrentProject?.Folder; }
        }

        public string FrameworkVersion
        {
            get { return String.IsNullOrEmpty(ModifyProject.CurrentProject?.FrameworkVersion) ? "???" : ModifyProject.CurrentProject.FrameworkVersion; }
        }

        public string CompanyName
        {
            get { return String.IsNullOrEmpty(ModifyProject.CurrentProject?.CompanyName) ? "???" : ModifyProject.CurrentProject.CompanyName; }
        }

        public string Name
        {
            get { return String.IsNullOrEmpty(ModifyProject.CurrentProject?.Name) ? "???" : ModifyProject.CurrentProject.Name; }
        }

        public string BIAFronts
        {
            get { return String.IsNullOrEmpty(ModifyProject.CurrentProject?.BIAFronts) ? "???" : ModifyProject.CurrentProject.BIAFronts; }
        }

        public Project CurrentProject
        {
            get { return ModifyProject.CurrentProject; }
            set
            {
                if (ModifyProject.CurrentProject != value)
                {
                    ModifyProject.CurrentProject = value;
                    RaisePropertyChanged("FrameworkVersion");
                    RaisePropertyChanged("CompanyName");
                    RaisePropertyChanged("Name");
                    RaisePropertyChanged("BIAFronts");
                }
            }
        }

    }
}
