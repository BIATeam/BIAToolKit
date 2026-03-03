namespace BIA.ToolKit.Dialogs
{
    using System.Windows;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.RegenerateFeatures;
    using BIA.ToolKit.Domain.ModifyProject;

    /// <summary>
    /// Interaction logic for RegenerateFeaturesDialog.xaml
    /// </summary>
    public partial class RegenerateFeaturesDialog : Window
    {
        public RegenerateFeaturesDialog(Project project, RegenerateFeaturesDiscoveryService discoveryService, UIEventBroker uiEventBroker, IConsoleWriter consoleWriter)
        {
            InitializeComponent();
            RegenerateFeatures.Inject(discoveryService, uiEventBroker, consoleWriter);
            RegenerateFeatures.SetCurrentProject(project);
        }
    }
}
