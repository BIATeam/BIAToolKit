namespace BIA.ToolKit.Dialogs
{
    using System;
    using System.Windows;
    using System.Windows.Input;

    /// <summary>
    /// Modal preview for a feature demo video. Sized to 80% of its owner window.
    /// Single-instance: opening a new preview closes any previous one.
    /// </summary>
    public partial class VideoPreviewWindow : Window
    {
        private static VideoPreviewWindow current;

        private VideoPreviewWindow(Uri videoSource)
        {
            InitializeComponent();
            PreviewMedia.Source = videoSource;
            Loaded += OnLoaded;
            Closed += OnClosed;
        }

        public static void Show(Window owner, Uri videoSource)
        {
            if (videoSource == null)
            {
                return;
            }

            current?.Close();

            var window = new VideoPreviewWindow(videoSource)
            {
                Owner = owner,
            };
            current = window;
            window.Show();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (Owner != null)
            {
                Width = Owner.ActualWidth * 0.8;
                Height = Owner.ActualHeight * 0.8;
            }

            PreviewMedia.Position = TimeSpan.Zero;
            PreviewMedia.Play();
        }

        private void OnClosed(object sender, EventArgs e)
        {
            PreviewMedia.Stop();
            PreviewMedia.Close();
            if (current == this)
            {
                current = null;
            }
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

        private void OnCloseExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }

        // Click on the dark backdrop (anywhere outside the inner content Border) closes the modal.
        private void OnBackdropClick(object sender, MouseButtonEventArgs e)
        {
            if (e.Source == sender)
            {
                Close();
            }
        }

        // Clicks on the content Border must NOT bubble up to the backdrop handler.
        private void OnContentClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }
    }
}
