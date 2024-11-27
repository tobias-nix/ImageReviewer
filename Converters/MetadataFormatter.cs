using System;
using System.Globalization;
using System.Windows.Data;
using ImageReviewer.Models;

namespace ImageReviewer.Converters
{
    public class MetadataFormatter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ImageMetadata metadata)
            {
                return metadata.GetFormattedInfo();
            }
            return "Keine Metadaten verfügbar";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
