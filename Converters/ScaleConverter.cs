using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Windows.Controls;

namespace ImageReviewer.Converters
{
    public class ScaleConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length != 3) return 0;

            if (values[0] is double containerSize && 
                values[1] is Image image && 
                values[2] is double rotation && 
                parameter is string scaleStr)
            {
                if (double.TryParse(scaleStr, out double scale))
                {
                    // Prüfe, ob es ein Landscape-Bild ist
                    bool isLandscape = false;
                    if (image.Source is BitmapSource source)
                    {
                        isLandscape = source.PixelWidth > source.PixelHeight;
                    }

                    // Bei 90° oder 270° Rotation und Landscape-Bild zusätzlich verkleinern
                    if (isLandscape && (rotation == 90 || rotation == 270))
                    {
                        scale *= 0.7; // Zusätzliche Verkleinerung für gedrehte Landscape-Bilder
                    }

                    return containerSize * scale;
                }
            }
            return values[0];
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
