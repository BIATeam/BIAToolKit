namespace BIA.ToolKit.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using BIA.ToolKit.Domain.Model;

    /// <summary>
    /// Maps a <see cref="MigrationStepStatus"/> to its display glyph.
    /// </summary>
    public sealed class MigrationStepStatusToGlyphConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not MigrationStepStatus status)
                return string.Empty;

            return status switch
            {
                MigrationStepStatus.Pending => "⏸",   // ⏸ pause
                MigrationStepStatus.Running => "⚡",   // ⚡ running
                MigrationStepStatus.Done => "✓",   // ✓ done
                MigrationStepStatus.Warning => "⚠",   // ⚠ warning
                MigrationStepStatus.Failed => "✗",   // ✗ failed
                _ => string.Empty,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
