using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace ImageReviewer.Models
{
    public class ImageMetadata
    {
        public string FileName { get; set; }
        public DateTime CreationDate { get; set; }
        public string CameraModel { get; set; }
        public string ExposureTime { get; set; }
        public string FNumber { get; set; }
        public string ISOSpeed { get; set; }
        public string FocalLength { get; set; }
        public string Resolution { get; set; }
        public long FileSize { get; set; }

        public static ImageMetadata FromFile(string filePath)
        {
            var metadata = new ImageMetadata
            {
                FileName = Path.GetFileName(filePath),
                CreationDate = File.GetCreationTime(filePath),
                FileSize = new FileInfo(filePath).Length
            };

            try
            {
                using (var image = Image.FromFile(filePath))
                {
                    metadata.Resolution = $"{image.Width} x {image.Height}";

                    foreach (var prop in image.PropertyItems)
                    {
                        switch (prop.Id)
                        {
                            case 0x010F: // Camera Make
                                metadata.CameraModel = Encoding.ASCII.GetString(prop.Value).Trim('\0');
                                break;
                            case 0x829A: // Exposure Time
                                var exposureTime = BitConverter.ToDouble(prop.Value, 0);
                                metadata.ExposureTime = $"1/{Math.Round(1 / exposureTime)} sec";
                                break;
                            case 0x829D: // F-Number
                                var fNumber = BitConverter.ToDouble(prop.Value, 0);
                                metadata.FNumber = $"f/{fNumber:F1}";
                                break;
                            case 0x8827: // ISO Speed
                                metadata.ISOSpeed = $"ISO {BitConverter.ToUInt16(prop.Value, 0)}";
                                break;
                            case 0x920A: // Focal Length
                                var focalLength = BitConverter.ToDouble(prop.Value, 0);
                                metadata.FocalLength = $"{focalLength:F1}mm";
                                break;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Wenn EXIF-Daten nicht gelesen werden können, bleiben die Felder leer
            }

            return metadata;
        }

        public string GetFormattedInfo()
        {
            var sb = new StringBuilder();
            
            if (!string.IsNullOrEmpty(Resolution))
                sb.AppendLine($"Auflösung: {Resolution}");
            
            if (!string.IsNullOrEmpty(CameraModel))
                sb.AppendLine($"Kamera: {CameraModel}");
            
            if (!string.IsNullOrEmpty(ExposureTime))
                sb.AppendLine($"Belichtungszeit: {ExposureTime}");
            
            if (!string.IsNullOrEmpty(FNumber))
                sb.AppendLine($"Blende: {FNumber}");
            
            if (!string.IsNullOrEmpty(ISOSpeed))
                sb.AppendLine($"ISO: {ISOSpeed}");
            
            if (!string.IsNullOrEmpty(FocalLength))
                sb.AppendLine($"Brennweite: {FocalLength}");
            
            sb.AppendLine($"Dateigröße: {FormatFileSize(FileSize)}");
            sb.AppendLine($"Erstellungsdatum: {CreationDate:dd.MM.yyyy HH:mm:ss}");

            return sb.ToString().Trim();
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;
            
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size = size / 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }
    }
}
