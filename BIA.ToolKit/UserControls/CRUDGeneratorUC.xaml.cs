namespace BIA.ToolKit.UserControls
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Helper;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for DtoGenerator.xaml
    /// </summary>
    public partial class CRUDGeneratorUC : UserControl
    {
        IConsoleWriter consoleWriter;
        CSharpParserService service;

        public CRUDGeneratorViewModel vm;


        /// <summary>
        /// Constructor
        /// </summary>
        public CRUDGeneratorUC()
        {
            InitializeComponent();
            vm = (CRUDGeneratorViewModel)base.DataContext;
        }

        public void Inject(CSharpParserService service, IConsoleWriter consoleWriter)
        {
            this.service = service;
            this.consoleWriter = consoleWriter;
        }

        #region Action
        private void ModifyDtoRootFile_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void ModifyDtoRootFileBrowse_Click(object sender, RoutedEventArgs e)
        {
            vm.DtoRootFilePath = FileDialog.BrowseFile(vm.DtoRootFilePath, "cs");
        }

        private void ModifyZipRootFile_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void ModifyZipRootFileBrowse_Click(object sender, RoutedEventArgs e)
        {
            vm.ZipRootFilePath = FileDialog.BrowseFile(vm.ZipRootFilePath, "zip");
        }

        private void ParseDto_Click(object sender, RoutedEventArgs e)
        {
            ParseDtoFile(vm.DtoRootFilePath);
        }

        private void ParseZip_Click(object sender, RoutedEventArgs e)
        {
            ParseZipFile(vm.ZipRootFilePath);
        }
        #endregion

        private void ParseDtoFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }

            CSharpParserService service = new CSharpParserService(new ConsoleWriter());
            EntityInfo dtoEntity = service.ParseEntity(fileName);

            if (dtoEntity != null && dtoEntity.Properties != null)
            {
                //dtoEntity.Properties.OrderBy(x => x.Name);
                vm.DtoProperties = dtoEntity.Properties.OrderBy(x => x.Name).ToList();
            }
            else
            {
                // TODO NMA: add logs
            }
        }

        private void ParseZipFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }

        }

    }


    //public class DataModelZip
    //{
    //    public List<string> dtoFiles { get; set; }
    //    public List<string> controlerFiles { get; set; }
    //    public List<string> applicationFiles { get; set; }
    //    public List<string> moduleFiles { get; set; }
    //}
}
