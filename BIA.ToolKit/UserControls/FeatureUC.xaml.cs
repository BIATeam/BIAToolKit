namespace BIA.ToolKit.UserControls
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
    using System.Windows.Navigation;
    using System.Windows.Shapes;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Application.ViewModel;

    /// <summary>
    /// Interaction logic for FeatureUC.xaml
    /// </summary>
    public partial class FeatureUC : UserControl
    {
        public FeatureUC()
        {
            InitializeComponent();
        }

        public ProjectWithParam GetProjectWithParam()
        {
            ProjectWithParam obj = new ProjectWithParam();

            FeatureViewModel vm = (FeatureViewModel)base.DataContext;

            obj.WithFrontEnd = vm.WithFrontEnd;
            obj.WithFrontFeature = vm.WithFrontFeature;
            obj.WithServiceApi = vm.WithServiceApi;
            obj.WithDeployDb = vm.WithDeployDb;
            obj.WithWorkerService = vm.WithWorkerService;
            obj.WithInfraData = vm.WithInfraData;

            return obj;

        }
    }
}
