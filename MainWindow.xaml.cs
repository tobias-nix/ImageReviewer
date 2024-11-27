using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Data;
using System.Windows.Threading;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Button = System.Windows.Controls.Button;
using ImageMetadata = ImageReviewer.Models.ImageMetadata; 
using ImageReviewer.Models;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;

namespace ImageReviewer
{
    public class SelectedImage
    {
        public BitmapSource Image { get; set; }
        public BitmapSource OriginalImage { get; set; }
        public string FilePath { get; set; }
        public string FileName { get; set; }
    }

    public class FilmstripItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        private string _fileName;
        private BitmapImage _thumbnail;
        private ImageMetadata _metadata;

        public BitmapImage Thumbnail
        {
            get => _thumbnail;
            set
            {
                _thumbnail = value;
                OnPropertyChanged(nameof(Thumbnail));
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

        public ImageMetadata Metadata
        {
            get => _metadata;
            set
            {
                _metadata = value;
                OnPropertyChanged(nameof(Metadata));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private List<string> _imageFiles;
        private int _currentImageIndex;
        private HashSet<string> _selectedImages;
        private ObservableCollection<SelectedImage> _selectedImagesList;
        private ObservableCollection<FilmstripItem> _filmstripItems;
        private readonly Dictionary<string, BitmapImage> _thumbnailCache = new();
        private readonly Dictionary<string, BitmapImage> _imageCache = new();
        private const int ThumbnailSize = 120;
        private const int MaxCacheSize = 100; // Maximale Anzahl der gecachten Bilder

        public enum SortOption
        {
            Name,
            Date,
            Size
        }

        private SortOption _currentSortOption = SortOption.Name;
        private bool _sortDescending = false;
        private string _exportPath;
        public string ExportPath
        {
            get => _exportPath ?? "Kein Zielordner ausgewählt";
            set
            {
                _exportPath = value;
                OnPropertyChanged(nameof(ExportPath));
            }
        }

        private string _currentPath;
        public string CurrentPath
        {
            get => _currentPath ?? "Kein Ordner ausgewählt";
            set
            {
                if (_currentPath != value)
                {
                    _currentPath = value;
                    OnPropertyChanged(nameof(CurrentPath));
                }
            }
        }

        private double _currentRotation = 0;
        public double CurrentRotation
        {
            get => _currentRotation;
            set
            {
                if (_currentRotation != value)
                {
                    _currentRotation = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentRotation)));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            _imageFiles = new List<string>();
            _selectedImages = new HashSet<string>();
            _selectedImagesList = new ObservableCollection<SelectedImage>();
            _filmstripItems = new ObservableCollection<FilmstripItem>();

            lvFilmstrip.ItemsSource = _filmstripItems;
            selectedImagesControl.ItemsSource = _selectedImagesList;

            btnSelectExportFolder.Click += BtnSelectExportFolder_Click;
            btnExport.Click += BtnExport_Click;

            PreviewKeyDown += MainWindow_PreviewKeyDown;
            lvFilmstrip.SelectionChanged += LvFilmstrip_SelectionChanged;
            cbSortOption.SelectionChanged += CbSortOption_SelectionChanged;
            btnSortDirection.Checked += BtnSortDirection_Checked;
        }

        private void AddToSelectedPanel(FilmstripItem item)
        {
            var thumbnailImage = item.Thumbnail;
            var originalImage = LoadImage(item.FilePath, 0);

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
                e.Handled = true;
            }
        }

        private void Image_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is FrameworkElement element && 
                element.Parent is Grid grid && 
                grid.Children.Count > 1 && 
                grid.Children[1] is Popup popup)
            {
                popup.IsOpen = true;
            }
        }

        private void Image_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is FrameworkElement element && 
                element.Parent is Grid grid && 
                grid.Children.Count > 1 && 
                grid.Children[1] is Popup popup)
            {
                popup.IsOpen = false;
            }
        }

        private async void BtnSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                ShowNewFolderButton = true,
                Description = "Wählen Sie einen Ordner mit Bildern aus",
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                CurrentPath = dialog.SelectedPath;
                await LoadImagesFromDirectory(CurrentPath);
            }
        }

        private async Task LoadImagesFromDirectory(string directoryPath)
        {
            try
            {
                loadingGrid.Visibility = Visibility.Visible;
                _filmstripItems.Clear();
                _thumbnailCache.Clear();
                _imageCache.Clear();

                var imageFiles = Directory.GetFiles(directoryPath)
                    .Where(file => file.ToLower().EndsWith(".jpg") || 
                                 file.ToLower().EndsWith(".jpeg") || 
                                 file.ToLower().EndsWith(".png") || 
                                 file.ToLower().EndsWith(".gif") || 
                                 file.ToLower().EndsWith(".bmp"))
                    .ToList();

                foreach (var file in imageFiles)
                {
                    var item = new FilmstripItem
                    {
                        FilePath = file,
                        FileName = Path.GetFileName(file),
                        IsSelected = false
                    };

                    await Task.Run(() =>
                    {
                        var thumbnail = LoadImage(file, ThumbnailSize);
                        var metadata = LoadMetadata(file);
                        Dispatcher.Invoke(() =>
                        {
                            item.Thumbnail = thumbnail;
                            item.Metadata = metadata;
                            _filmstripItems.Add(item);
                        });
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Bilder: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                loadingGrid.Visibility = Visibility.Collapsed;
            }
        }

        private BitmapImage LoadImage(string path, int maxSize)
        {
            try
            {
                var cache = maxSize == 0 ? _imageCache : _thumbnailCache;
                
                if (cache.TryGetValue(path, out var cachedImage))
                {
                    return cachedImage;
                }

                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                image.UriSource = new Uri(path);
                
                if (maxSize > 0)
                {
                    image.DecodePixelWidth = maxSize;
                }
                
                image.EndInit();
                image.Freeze(); // Macht das Bild threadsicher und verbessert die Performance

                // Cache-Größe überprüfen und ggf. älteste Einträge entfernen
                if (cache.Count >= MaxCacheSize)
                {
                    var oldestKey = cache.Keys.First();
                    cache.Remove(oldestKey);
                }

                cache[path] = image;
                return image;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Laden des Bildes {path}: {ex.Message}");
                return null;
            }
        }

        private ImageMetadata LoadMetadata(string path)
        {
            try
            {
                var metadata = new ImageMetadata
                {
                    FileName = Path.GetFileName(path),
                    CreationDate = File.GetCreationTime(path),
                    FileSize = new FileInfo(path).Length
                };

                using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    var bitmapDecoder = BitmapDecoder.Create(fileStream, BitmapCreateOptions.IgnoreImageCache, BitmapCacheOption.None);
                    var frame = bitmapDecoder.Frames[0];
                    var metadataReader = frame.Metadata as BitmapMetadata;
                    if (metadataReader != null && !string.IsNullOrEmpty(metadataReader.DateTaken))
                    {
                        if (DateTime.TryParse(metadataReader.DateTaken, out DateTime creationDate))
                        {
                            metadata.CreationDate = creationDate;
                        }
                    }
                }
                return metadata;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Laden der Metadaten für {path}: {ex.Message}");
                return new ImageMetadata
                {
                    FileName = Path.GetFileName(path),
                    CreationDate = File.GetCreationTime(path),
                    FileSize = new FileInfo(path).Length
                };
            }
        }

        private void LvFilmstrip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lvFilmstrip.SelectedItem is FilmstripItem item)
            {
                var image = LoadImage(item.FilePath, 0);
                if (image != null)
                {
                    imgMain.Source = image;
                    ResetImageTransforms();
                }
                else
                {
                    MessageBox.Show("Fehler beim Laden des Bildes.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                lvFilmstrip.ScrollIntoView(item);
            }
        }

        private void ResetImageTransforms()
        {
            if (imgMain != null)
            {
                imgMain.RenderTransform = null;
                CurrentRotation = 0;
            }
        }

        private void CbSortOption_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbSortOption.SelectedItem is ComboBoxItem selectedItem)
            {
                var sortOption = selectedItem.Content.ToString() switch
                {
                    "Name" => SortOption.Name,
                    "Datum" => SortOption.Date,
                    "Größe" => SortOption.Size,
                    _ => SortOption.Name
                };
                SortImages(sortOption);
            }
        }

        private void SortImages(SortOption sortOption)
        {
            if (_filmstripItems == null || !_filmstripItems.Any()) return;

            _currentSortOption = sortOption;
            var sortedItems = sortOption switch
            {
                SortOption.Name => _sortDescending 
                    ? _filmstripItems.OrderByDescending(x => x.FileName)
                    : _filmstripItems.OrderBy(x => x.FileName),
                SortOption.Date => _sortDescending
                    ? _filmstripItems.OrderByDescending(x => x.Metadata?.CreationDate ?? DateTime.MinValue)
                    : _filmstripItems.OrderBy(x => x.Metadata?.CreationDate ?? DateTime.MinValue),
                SortOption.Size => _sortDescending
                    ? _filmstripItems.OrderByDescending(x => x.Metadata?.FileSize ?? 0)
                    : _filmstripItems.OrderBy(x => x.Metadata?.FileSize ?? 0),
                _ => _sortDescending
                    ? _filmstripItems.OrderByDescending(x => x.FileName)
                    : _filmstripItems.OrderBy(x => x.FileName)
            };

            var sorted = sortedItems.ToList();
            _filmstripItems.Clear();
            foreach (var item in sorted)
            {
                _filmstripItems.Add(item);
            }
        }

        private void BtnSortDirection_Checked(object sender, RoutedEventArgs e)
        {
            _sortDescending = btnSortDirection.IsChecked ?? false;
            sortIcon.Kind = _sortDescending ? PackIconKind.SortDescending : PackIconKind.SortAscending;
            if (_filmstripItems != null && _filmstripItems.Any())
            {
                SortImages(_currentSortOption);
            }
        }

        private void BtnSelectExportFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            var result = dialog.ShowDialog();
            
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                ExportPath = dialog.SelectedPath;
            }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ExportPath))
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
                    string targetPath = Path.Combine(ExportPath, fileName);
                    File.Copy(sourcePath, targetPath, true);
                }

                MessageBox.Show($"{_selectedImages.Count} Bilder wurden erfolgreich exportiert.", "Export erfolgreich", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Exportieren der Bilder: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void txtCurrentPath_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void BtnRemoveImage_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string filePath)
            {
                _selectedImages.Remove(filePath);
                var imageToRemove = _selectedImagesList.FirstOrDefault(x => x.FilePath == filePath);
                if (imageToRemove != null)
                {
                    _selectedImagesList.Remove(imageToRemove);
                }
            }
        }

        private void BtnRotateImage_Click(object sender, RoutedEventArgs e)
        {
            if (imgMain.Source != null)
            {
                // Erstelle eine neue TransformGroup, falls noch keine existiert
                if (imgMain.RenderTransform == null || !(imgMain.RenderTransform is TransformGroup))
                {
                    imgMain.RenderTransform = new TransformGroup();
                }

                var transformGroup = (TransformGroup)imgMain.RenderTransform;
                
                // Suche nach einer existierenden RotateTransform oder erstelle eine neue
                var rotateTransform = transformGroup.Children.OfType<RotateTransform>().FirstOrDefault();
                if (rotateTransform == null)
                {
                    rotateTransform = new RotateTransform();
                    transformGroup.Children.Add(rotateTransform);
                }

                // Rotiere um weitere 90 Grad
                CurrentRotation = (CurrentRotation + 90) % 360;
                rotateTransform.Angle = CurrentRotation;
            }
        }
    }
}