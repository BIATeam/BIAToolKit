namespace BIA.ToolKit.Dialogs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Shapes;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Domain.Settings;

    /// <summary>
    /// Interaction logic for LocalReleaseRepositorySettings.xaml
    /// </summary>
    public partial class LocalReleaseRepositorySettingsUC : Window
    {
        private readonly LocalReleaseRepositorySettingsViewModel viewModel;
        public LocalReleaseRepositorySettingsUC()
        {
            InitializeComponent();
            viewModel = (LocalReleaseRepositorySettingsViewModel)DataContext;
        }

        internal new bool? ShowDialog(BIATKSettings settings)
        {
            viewModel.LoadSettings(settings);
            return ShowDialog();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
