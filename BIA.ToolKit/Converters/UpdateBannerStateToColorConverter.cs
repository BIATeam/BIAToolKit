namespace BIA.ToolKit.Converters
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Media;
    using BIA.ToolKit.Domain.Model;

    /// <summary>
    /// Maps an <see cref="UpdateBannerState"/> to a <see cref="Brush"/>.
    /// Resolves through MaterialDesign theme resources when available so
    /// dark/light theme switching propagates automatically; falls back to
    /// fixed RGB values for design-time preview.
    /// </summary>
    public sealed class UpdateBannerStateToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not UpdateBannerState state)
                return Brushes.Gray;

            string resourceKey = state switch
            {
                UpdateBannerState.UpToDate => "MaterialDesign.Brush.ValidationSuccess",
                UpdateBannerState.UpdateAvailable => "MaterialDesign.Brush.ValidationWarning",
                UpdateBannerState.Checking => "MaterialDesign.Brush.Primary",
                UpdateBannerState.NoSource => "MaterialDesign.Brush.Foreground",
                UpdateBannerState.Failed => "MaterialDesign.Brush.ValidationError",
                _ => "MaterialDesign.Brush.Foreground",
            };

            object resource = Application.Current?.TryFindResource(resourceKey);
            if (resource is Brush brush)
                return brush;

            return state switch
            {
                UpdateBannerState.UpToDate => new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)),
                UpdateBannerState.UpdateAvailable => new SolidColorBrush(Color.FromRgb(0xFF, 0xA7, 0x26)),
                UpdateBannerState.Checking => new SolidColorBrush(Color.FromRgb(0x42, 0xA5, 0xF5)),
                UpdateBannerState.NoSource => new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E)),
                UpdateBannerState.Failed => new SolidColorBrush(Color.FromRgb(0xEF, 0x53, 0x50)),
                _ => Brushes.Gray,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
