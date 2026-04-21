namespace BIA.ToolKit.UserControls
{
    using System;
    using System.IO;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Self-contained looping video preview for a CRUD feature tile.
    /// Resolves its source from Resources/Previews/{FeatureName}.(mp4|webm);
    /// hides itself silently if no matching file is bundled.
    /// </summary>
    public partial class FeaturePreviewVideo : UserControl
    {
        private static readonly string[] SupportedExtensions = [".mp4", ".webm", ".wmv"];

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
            if (string.IsNullOrWhiteSpace(FeatureName))
            {
                Container.Visibility = Visibility.Collapsed;
                return;
            }

            var previewsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Previews");
            foreach (var ext in SupportedExtensions)
            {
                var candidate = Path.Combine(previewsDir, FeatureName + ext);
                if (File.Exists(candidate))
                {
                    PreviewMedia.Source = new Uri(candidate, UriKind.Absolute);
                    Container.Visibility = Visibility.Visible;
                    return;
                }
            }

            Container.Visibility = Visibility.Collapsed;
        }

        private void Media_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (PreviewMedia.Source != null)
            {
                PreviewMedia.Position = TimeSpan.Zero;
                PreviewMedia.Play();
            }
        }

        private void Media_OnUnloaded(object sender, RoutedEventArgs e)
        {
            PreviewMedia.Pause();
        }

        private void Media_OnOpened(object sender, RoutedEventArgs e)
        {
            PreviewMedia.Play();
        }

        private void Media_OnEnded(object sender, RoutedEventArgs e)
        {
            PreviewMedia.Position = TimeSpan.Zero;
            PreviewMedia.Play();
        }

        private void Media_OnFailed(object sender, ExceptionRoutedEventArgs e)
        {
            // Codec missing or file corrupt — fail silently, hide the block.
            Container.Visibility = Visibility.Collapsed;
        }
    }
}
