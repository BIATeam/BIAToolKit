namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Domain.DtoGenerator;
    using System.IO;

    public class DtoGeneratorViewModel : ObservableObject
    {
        /// <summary>
        /// NOTE: You need a parameterless     
        /// constructor for post-backs in MVC    
        /// </summary>
        public DtoGeneratorViewModel()
        {
            DtoGenerator = new DtoGenerator();
        }
        /*
        public DtoGeneratorViewModel(IProductRepository repository)
        {
            //Repository = repository;
        }*/

        //public IProductRepository Repository { get; set; }
        public DtoGenerator DtoGenerator { get; set; }

        public void InitProject(string rootFolder, string projectCompanyName = "", string projectName = "")
        {
            string path = rootFolder;
            if (Directory.Exists(path))
            {
                DtoGenerator.ProjectDir = path;
                path = DtoGenerator.ProjectDir + "\\" + projectName;
                if (Directory.Exists(path))
                {
                    DtoGenerator.ProjectDir = path;
                    path = DtoGenerator.ProjectDir + "\\DotNet";
                    if (Directory.Exists(path))
                    {
                        DtoGenerator.ProjectDir = path;
                        path = DtoGenerator.ProjectDir + "\\" + projectCompanyName + "." + projectName + ".Domain";
                        if (Directory.Exists(path))
                        {
                            DtoGenerator.ProjectDir = path;
                            path = DtoGenerator.ProjectDir + "\\" + projectCompanyName + "." + projectName + ".Domain.csproj";
                            if (File.Exists(path))
                            {
                                DtoGenerator.ProjectPath = path;
                            }
                        }
                    }
                }
            }
        }
    }
}
