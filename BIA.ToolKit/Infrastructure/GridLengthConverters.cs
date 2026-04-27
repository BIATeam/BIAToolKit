namespace BIA.ToolKit.Infrastructure
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;

    /// <summary>
    /// Converts a bool to a <see cref="GridLength"/>.
    /// Behavior depends on ConverterParameter:
    /// - "TrueZero" (default): true → 0, false → * (collapse row when flag on)
    /// - "TrueStar": true → *, false → auto (grow row when flag on)
    /// </summary>
    public class BoolToGridLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = value is bool b && b;
            string mode = parameter as string ?? "TrueZero";
            return mode switch
            {
                "TrueStar" => flag ? new GridLength(1, GridUnitType.Star) : GridLength.Auto,
                _ => flag ? new GridLength(0) : new GridLength(1, GridUnitType.Star)
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
