namespace BIA.ToolKit.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using BIA.ToolKit.Domain;

    /// <summary>
    /// Maps a <see cref="RepositoryType"/> to its short badge text
    /// (icon glyph + label) shown at the right of the card header.
    /// </summary>
    public sealed class RepositoryTypeToBadgeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not RepositoryType type)
                return string.Empty;

            return type switch
            {
                RepositoryType.Git => "🌐 Git",
                RepositoryType.Folder => "📁 Folder",
                _ => string.Empty,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
