namespace BIA.ToolKit.Infrastructure
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;

    /// <summary>
    /// Returns Visible when the bound width is >= the threshold (ConverterParameter),
    /// Collapsed otherwise. Used to hide button labels at narrow window widths while
    /// keeping the icons.
    /// </summary>
    public class WidthToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double width
                && parameter is string p
                && double.TryParse(p, NumberStyles.Float, CultureInfo.InvariantCulture, out var threshold))
            {
                if (width <= 0)
                {
                    return Visibility.Visible;
                }
                return width >= threshold ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
