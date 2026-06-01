namespace BIA.ToolKit.Converters
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Media;
    using BIA.ToolKit.Domain.Model;

    /// <summary>
    /// Maps a <see cref="RepositorySyncStatus"/> to the card border brush.
    /// Idle uses the card's default border resource; Syncing tints primary;
    /// Failed tints orange.
    /// </summary>
    public sealed class RepositorySyncStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not RepositorySyncStatus status)
                return Brushes.Transparent;

            string resourceKey = status switch
            {
                RepositorySyncStatus.Syncing => "MaterialDesign.Brush.Primary",
                RepositorySyncStatus.Failed => "MaterialDesign.Brush.ValidationWarning",
                _ => "MaterialDesign.Brush.Card.Border",
            };

            object resource = Application.Current?.TryFindResource(resourceKey);
            if (resource is Brush brush)
                return brush;

            return status switch
            {
                RepositorySyncStatus.Syncing => new SolidColorBrush(Color.FromRgb(0x42, 0xA5, 0xF5)),
                RepositorySyncStatus.Failed => new SolidColorBrush(Color.FromRgb(0xFF, 0xA7, 0x26)),
                _ => Brushes.Transparent,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
