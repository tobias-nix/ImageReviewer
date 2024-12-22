using System;
using System.Globalization;
using System.Text;
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
                var sb = new StringBuilder();
                sb.AppendLine($"Datei: {metadata.FileName}");
                sb.AppendLine($"Typ: {metadata.FileType}");
                sb.AppendLine($"Aufnahmedatum: {metadata.CreationDate:dd.MM.yyyy HH:mm:ss}");
                sb.AppendLine($"Größe: {FormatFileSize(metadata.FileSize)}");

                if (!string.IsNullOrEmpty(metadata.CameraModel))
                    sb.AppendLine($"Kamera: {metadata.CameraModel}");
                
                if (!string.IsNullOrEmpty(metadata.LensModel))
                    sb.AppendLine($"Objektiv: {metadata.LensModel}");

                if (!string.IsNullOrEmpty(metadata.ExposureTime))
                    sb.AppendLine($"Belichtungszeit: {metadata.ExposureTime}");
                
                if (!string.IsNullOrEmpty(metadata.FNumber))
                    sb.AppendLine($"Blende: {metadata.FNumber}");
                
                if (!string.IsNullOrEmpty(metadata.ISOSpeed))
                    sb.AppendLine($"ISO: {metadata.ISOSpeed}");

                return sb.ToString().TrimEnd();
            }
            return string.Empty;
        }

        private string FormatFileSize(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB" };
            double size = bytes;
            int unit = 0;

            while (size >= 1024 && unit < units.Length - 1)
            {
                size /= 1024;
                unit++;
            }

            return $"{size:0.##} {units[unit]}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
