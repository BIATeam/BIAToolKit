namespace BIA.ToolKit.UserControls
{
    using System.Windows.Controls;
    using BIA.ToolKit.ViewModel;

    /// <summary>
    /// Interaction logic for ModifyProjectUC.xaml
    /// </summary>
    public partial class ModifyProjectUC : ViewModelUserControl<ModifyProjectViewModel>
    {
        public ModifyProjectUC()
        {
            InitializeComponent();
        }

        private void ModifyProjectRootFolderText_TextChanged(object sender, TextChangedEventArgs e)
        {
            ViewModel?.ParameterModifyChange();
        }
    }
}
