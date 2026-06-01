namespace BIA.ToolKit.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using BIA.ToolKit.Domain.Model;

    /// <summary>
    /// Maps an <see cref="UpdateBannerState"/> to its banner glyph.
    /// </summary>
    public sealed class UpdateBannerStateToGlyphConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not UpdateBannerState state)
                return string.Empty;

            return state switch
            {
                UpdateBannerState.UpToDate => "✓",
                UpdateBannerState.UpdateAvailable => "⚠",
                UpdateBannerState.Checking => "⏳",
                UpdateBannerState.NoSource => "🔧",
                UpdateBannerState.Failed => "✗",
                _ => string.Empty,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
