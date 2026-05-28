namespace BIA.ToolKit.Converters
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Media;
    using BIA.ToolKit.Domain.Model;

    /// <summary>
    /// Maps a <see cref="MigrationStepStatus"/> to a <see cref="Brush"/>.
    /// Resolves colors through the application's MaterialDesign resources so
    /// dark / light theme switches propagate automatically.
    /// Pending uses a muted MaterialDesign foreground; other states use
    /// MaterialDesign Primary / Validation Success / Validation Warning /
    /// Validation Error brushes when available, falling back to fixed colors.
    /// </summary>
    public sealed class MigrationStepStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not MigrationStepStatus status)
                return Brushes.Gray;

            string resourceKey = status switch
            {
                MigrationStepStatus.Pending => "MaterialDesign.Brush.Foreground",
                MigrationStepStatus.Running => "MaterialDesign.Brush.Primary",
                MigrationStepStatus.Done => "MaterialDesign.Brush.ValidationSuccess",
                MigrationStepStatus.Warning => "MaterialDesign.Brush.ValidationWarning",
                MigrationStepStatus.Failed => "MaterialDesign.Brush.ValidationError",
                _ => "MaterialDesign.Brush.Foreground",
            };

            object resource = Application.Current?.TryFindResource(resourceKey);
            if (resource is Brush brush)
                return brush;

            // Hard-coded fallback if MaterialDesign theme is not loaded
            // (e.g. design-time preview). Matches the spec's dark-theme palette.
            return status switch
            {
                MigrationStepStatus.Pending => new SolidColorBrush(Color.FromRgb(0xB0, 0xB0, 0xB0)),
                MigrationStepStatus.Running => new SolidColorBrush(Color.FromRgb(0x42, 0xA5, 0xF5)),
                MigrationStepStatus.Done => new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)),
                MigrationStepStatus.Warning => new SolidColorBrush(Color.FromRgb(0xFF, 0xA7, 0x26)),
                MigrationStepStatus.Failed => new SolidColorBrush(Color.FromRgb(0xEF, 0x53, 0x50)),
                _ => Brushes.Gray,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
