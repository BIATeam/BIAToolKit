namespace BIA.ToolKit.UserControls
{
    using System;
    using System.IO;
    using System.Windows;
    using System.Windows.Controls;
    using BIA.ToolKit.Dialogs;

    /// <summary>
    /// Compact "Watch demo" button for a CRUD feature tile. Resolves its source
    /// from Resources/Previews/{FeatureName}.(mp4|webm|wmv) and hides itself
    /// silently if no matching file is bundled. Clicking opens a modal preview
    /// window sized to 80% of the main window.
    /// </summary>
    public partial class FeaturePreviewVideo : UserControl
    {
        private static readonly string[] SupportedExtensions = [".mp4", ".webm", ".wmv"];

        private Uri resolvedSource;

        public FeaturePreviewVideo()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty FeatureNameProperty =
            DependencyProperty.Register(
                nameof(FeatureName),
                typeof(string),
                typeof(FeaturePreviewVideo),
                new PropertyMetadata(null, OnFeatureNameChanged));

        public string FeatureName
        {
            get => (string)GetValue(FeatureNameProperty);
            set => SetValue(FeatureNameProperty, value);
        }

        private static void OnFeatureNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((FeaturePreviewVideo)d).ResolveSource();
        }

        private void ResolveSource()
        {
            resolvedSource = null;

            if (string.IsNullOrWhiteSpace(FeatureName))
            {
                PlayButton.Visibility = Visibility.Collapsed;
                return;
            }

            var previewsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Previews");
            foreach (var ext in SupportedExtensions)
            {
                var candidate = Path.Combine(previewsDir, FeatureName + ext);
                if (File.Exists(candidate))
                {
                    resolvedSource = new Uri(candidate, UriKind.Absolute);
                    PlayButton.Visibility = Visibility.Visible;
                    return;
                }
            }

            PlayButton.Visibility = Visibility.Collapsed;
        }

        private void OnPlayClick(object sender, RoutedEventArgs e)
        {
            if (resolvedSource == null)
            {
                return;
            }

            VideoPreviewWindow.Show(Window.GetWindow(this), resolvedSource);
        }
    }
}
