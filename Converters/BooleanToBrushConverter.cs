using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ImageReviewer
{
    public class BooleanToBrushConverter : IValueConverter
    {
        public required Brush TrueBrush { get; set; }
        public required Brush FalseBrush { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? TrueBrush : FalseBrush;
            }
            return FalseBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
