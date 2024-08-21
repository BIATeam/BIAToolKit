namespace BIA.ToolKit.UserControls
{
    using System.Windows;
    using System.Windows.Controls;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Application.ViewModel;

    /// <summary>
    /// Interaction logic for FeatureUC.xaml
    /// </summary>
    public partial class FeatureUC : UserControl
    {
        public FeatureSettingVM ViewModel
        {
            get { return (FeatureSettingVM)DataContext; }
            set { DataContext = value; }
        }

        public FeatureUC()
        {
            InitializeComponent();
        }
    }
}
