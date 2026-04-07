namespace BIA.ToolKit.UserControls
{
    using System.Windows.Controls;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Messages;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.ModifyProject;
    using CommunityToolkit.Mvvm.Messaging;

    /// <summary>
    /// Interaction logic for CRUDGeneratorUC.xaml
    /// </summary>
    public partial class CRUDGeneratorUC : UserControl
    {
        private readonly CRUDGeneratorViewModel vm;

        /// <summary>
        /// Constructor
        /// </summary>
        public CRUDGeneratorUC()
        {
            InitializeComponent();
            vm = (CRUDGeneratorViewModel)base.DataContext;
        }

        /// <summary>
        /// Injection of services.
        /// </summary>
        public void Inject(CSharpParserService service, ZipParserService zipService, GenerateCrudService crudService, SettingsService settingsService,
            IConsoleWriter consoleWriter, FileGeneratorService fileGeneratorService)
        {
            WeakReferenceMessenger.Default.Register<ProjectChangedMessage>(this, (r, m) => vm.SetCurrentProject(m.Project));
            WeakReferenceMessenger.Default.Register<SolutionClassesParsedMessage>(this, (r, m) => vm.OnSolutionClassesParsed());

            vm.Inject(service, zipService, crudService, settingsService, consoleWriter, fileGeneratorService, App.GetService<IDialogService>());
        }
    }
}
