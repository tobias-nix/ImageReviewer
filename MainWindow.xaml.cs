using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Data;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace ImageReviewer
{
    public class SelectedImage
    {
        public BitmapSource Image { get; set; }
        public BitmapSource OriginalImage { get; set; }
        public string FilePath { get; set; }
    }

    public class FilmstripItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        private string _fileName;
        private BitmapImage _image;

        public BitmapImage Image
        {
            get => _image;
            set
            {
                _image = value;
                OnPropertyChanged(nameof(Image));
            }
        }

        public string FilePath { get; set; }

        public string FileName
        {
            get => _fileName;
            set
            {
                _fileName = value;
                OnPropertyChanged(nameof(FileName));
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public partial class MainWindow : Window
    {
        private List<string> _imageFiles;
        private int _currentImageIndex;
        private HashSet<string> _selectedImages;
        private ObservableCollection<SelectedImage> _selectedImagesList;
        private ObservableCollection<FilmstripItem> _filmstripItems;

        public MainWindow()
        {
            InitializeComponent();
            _imageFiles = new List<string>();
            _selectedImages = new HashSet<string>();
            _selectedImagesList = new ObservableCollection<SelectedImage>();
            _filmstripItems = new ObservableCollection<FilmstripItem>();
            selectedImagesControl.ItemsSource = _selectedImagesList;
            lvFilmstrip.ItemsSource = _filmstripItems;

            btnSelectFolder.Click += BtnSelectFolder_Click;
            btnSelectExportFolder.Click += BtnSelectExportFolder_Click;
            btnExport.Click += BtnExport_Click;

            PreviewKeyDown += MainWindow_PreviewKeyDown;
            lvFilmstrip.SelectionChanged += LvFilmstrip_SelectionChanged;
        }

        private void Image_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                var grid = border.Parent as Grid;
                if (grid != null)
                {
                    var popup = grid.Children.OfType<Popup>().FirstOrDefault();
                    if (popup != null)
                    {
                        popup.IsOpen = true;
                    }
                }
            }
        }

        private void Image_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                var grid = border.Parent as Grid;
                if (grid != null)
                {
                    var popup = grid.Children.OfType<Popup>().FirstOrDefault();
                    if (popup != null)
                    {
                        popup.IsOpen = false;
                    }
                }
            }
        }

        private async void BtnSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    txtCurrentPath.Text = dialog.SelectedPath;
                    await LoadImagesFromDirectory(dialog.SelectedPath);
                }
            }
        }

        private async Task LoadImagesFromDirectory(string directoryPath)
        {
            _filmstripItems.Clear();
            _selectedImages.Clear();
            _selectedImagesList.Clear();

            var imageFiles = Directory.GetFiles(directoryPath)
                .Where(file => file.ToLower().EndsWith(".jpg") || 
                              file.ToLower().EndsWith(".jpeg") || 
                              file.ToLower().EndsWith(".png") || 
                              file.ToLower().EndsWith(".gif"))
                .ToList();

            if (imageFiles.Count == 0)
            {
                MessageBox.Show("Keine Bilder im ausgewählten Ordner gefunden.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            loadingGrid.Visibility = Visibility.Visible;

            foreach (var imagePath in imageFiles)
            {
                var item = new FilmstripItem
                {
                    FilePath = imagePath,
                    FileName = Path.GetFileName(imagePath),
                    Image = LoadImage(imagePath, 100),
                    IsSelected = false
                };
                _filmstripItems.Add(item);
            }

            if (_filmstripItems.Count > 0)
            {
                lvFilmstrip.SelectedIndex = 0;
            }

            loadingGrid.Visibility = Visibility.Collapsed;
        }

        private bool IsImageFile(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLower();
            return new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" }.Contains(ext);
        }

        private void DisplayCurrentImage()
        {
            if (_currentImageIndex >= 0 && _currentImageIndex < _imageFiles.Count)
            {
                imgMain.Source = LoadImage(_imageFiles[_currentImageIndex], 0);
                lvFilmstrip.SelectedIndex = _currentImageIndex;
            }
        }

        private BitmapSource LoadImage(string imagePath, int decodeHeight, bool keepOriginalSize = false)
        {
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(imagePath);
            if (decodeHeight > 0 && !keepOriginalSize)
            {
                image.DecodePixelHeight = decodeHeight;
            }
            image.EndInit();
            image.Freeze();

            // Bildausrichtung aus EXIF-Daten lesen
            int orientation = 1;
            try
            {
                using (Stream imageStream = File.OpenRead(imagePath))
                {
                    BitmapDecoder decoder = BitmapDecoder.Create(imageStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.None);
                    if (decoder.Frames[0].Metadata is BitmapMetadata metadata && metadata.ContainsQuery("/app1/ifd/{ushort=274}"))
                    {
                        orientation = (int)metadata.GetQuery("/app1/ifd/{ushort=274}");
                    }
                }
            }
            catch
            {
                // Falls die EXIF-Daten nicht gelesen werden können, verwenden wir die Standardausrichtung
            }

            // Transformation basierend auf der Orientierung anwenden
            TransformedBitmap transformedBitmap = new TransformedBitmap();
            transformedBitmap.BeginInit();
            transformedBitmap.Source = image;

            Transform transform = null;
            switch (orientation)
            {
                case 2: // Horizontal spiegeln
                    transform = new ScaleTransform(-1, 1);
                    break;
                case 3: // 180° rotieren
                    transform = new RotateTransform(180);
                    break;
                case 4: // Vertikal spiegeln
                    transform = new ScaleTransform(1, -1);
                    break;
                case 5: // Horizontal spiegeln und 270° rotieren
                    transform = new TransformGroup
                    {
                        Children = new TransformCollection
                        {
                            new ScaleTransform(-1, 1),
                            new RotateTransform(270)
                        }
                    };
                    break;
                case 6: // 90° rotieren
                    transform = new RotateTransform(90);
                    break;
                case 7: // Horizontal spiegeln und 90° rotieren
                    transform = new TransformGroup
                    {
                        Children = new TransformCollection
                        {
                            new ScaleTransform(-1, 1),
                            new RotateTransform(90)
                        }
                    };
                    break;
                case 8: // 270° rotieren
                    transform = new RotateTransform(270);
                    break;
                default:
                    return image;
            }

            if (transform != null)
            {
                transformedBitmap.Transform = transform;
                transformedBitmap.EndInit();
                transformedBitmap.Freeze();
                return transformedBitmap;
            }

            return image;
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space && lvFilmstrip.SelectedItem is FilmstripItem item)
            {
                if (_selectedImages.Contains(item.FilePath))
                {
                    _selectedImages.Remove(item.FilePath);
                    RemoveFromSelectedPanel(item);
                }
                else
                {
                    _selectedImages.Add(item.FilePath);
                    AddToSelectedPanel(item);
                }
                item.IsSelected = !item.IsSelected;
                CollectionViewSource.GetDefaultView(_filmstripItems).Refresh();
            }
            else if (e.Key == Key.Left && lvFilmstrip.SelectedIndex > 0)
            {
                lvFilmstrip.SelectedIndex--;
            }
            else if (e.Key == Key.Right && lvFilmstrip.SelectedIndex < _filmstripItems.Count - 1)
            {
                lvFilmstrip.SelectedIndex++;
            }
        }

        private void ToggleImageSelection()
        {
            if (lvFilmstrip.SelectedItem is FilmstripItem item)
            {
                item.IsSelected = !item.IsSelected;
                
                if (item.IsSelected)
                {
                    AddToSelectedPanel(item);
                }
                else
                {
                    RemoveFromSelectedPanel(item);
                }

                CenterSelectedItem();
            }
        }

        private void AddToSelectedPanel(string imagePath)
        {
            var thumbnailImage = LoadImage(imagePath, 100);
            var originalImage = LoadImage(imagePath, 0, true);

            _selectedImagesList.Add(new SelectedImage
            {
                Image = thumbnailImage,
                OriginalImage = originalImage,
                FilePath = imagePath
            });
        }

        private void AddToSelectedPanel(FilmstripItem item)
        {
            var thumbnailImage = item.Image;
            var originalImage = LoadImage(item.FilePath, 0, true);

            _selectedImagesList.Add(new SelectedImage
            {
                Image = thumbnailImage,
                OriginalImage = originalImage,
                FilePath = item.FilePath
            });
        }

        private void RemoveFromSelectedPanel(FilmstripItem item)
        {
            var imageToRemove = _selectedImagesList.FirstOrDefault(img => img.FilePath == item.FilePath);
            if (imageToRemove != null)
            {
                _selectedImagesList.Remove(imageToRemove);
            }
        }

        private void LvFilmstrip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lvFilmstrip.SelectedItem is FilmstripItem item)
            {
                imgMain.Source = LoadImage(item.FilePath, 0);
                CenterSelectedItem();
            }
        }

        private void CenterSelectedItem()
        {
            if (lvFilmstrip.SelectedItem != null)
            {
                lvFilmstrip.ScrollIntoView(lvFilmstrip.SelectedItem);
                
                // Warte kurz, bis das Scrollen abgeschlossen ist
                Task.Delay(50).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        var selectedIndex = lvFilmstrip.SelectedIndex;
                        if (selectedIndex >= 0)
                        {
                            var scrollViewer = GetScrollViewer(lvFilmstrip);
                            if (scrollViewer != null)
                            {
                                var itemWidth = 120; // Ungefähre Breite eines Items inkl. Margin
                                var offset = selectedIndex * itemWidth;
                                var viewportWidth = scrollViewer.ViewportWidth;
                                
                                // Zentriere das ausgewählte Item
                                scrollViewer.ScrollToHorizontalOffset(offset - (viewportWidth - itemWidth) / 2);
                            }
                        }
                    });
                });
            }
        }

        private ScrollViewer GetScrollViewer(DependencyObject element)
        {
            if (element is ScrollViewer scrollViewer)
                return scrollViewer;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                var child = VisualTreeHelper.GetChild(element, i);
                var result = GetScrollViewer(child);
                if (result != null)
                    return result;
            }

            return null;
        }

        private void BtnSelectExportFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    txtExportPath.Text = dialog.SelectedPath;
                }
            }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtExportPath.Text))
            {
                MessageBox.Show("Bitte wählen Sie zuerst einen Zielordner aus.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_selectedImages.Count == 0)
            {
                MessageBox.Show("Es wurden keine Bilder zum Exportieren ausgewählt.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                foreach (string sourcePath in _selectedImages)
                {
                    string fileName = Path.GetFileName(sourcePath);
                    string targetPath = Path.Combine(txtExportPath.Text, fileName);
                    File.Copy(sourcePath, targetPath, true);
                }

                MessageBox.Show($"{_selectedImages.Count} Bilder wurden erfolgreich exportiert.", "Export erfolgreich", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Exportieren der Bilder: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}