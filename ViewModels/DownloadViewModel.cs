using System;
using System.Collections.ObjectModel;
using System.IO;
using ReactiveUI;

namespace LorealAvaloniaUI.ViewModels
{
    public class DownloadViewModel : ReactiveObject
    {
        public ObservableCollection<FileItemViewModel> Files { get; } = new ObservableCollection<FileItemViewModel>();

        public DownloadViewModel()
        {
            string downloadsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads"
            );

            if (!Directory.Exists(downloadsPath))
            {
                Console.WriteLine("Downloads directory does not exist!");
                return;
            }

            try
            {
                const long OneMB = 1048576;
                var allFiles = Directory.GetFiles(downloadsPath, "*.*", SearchOption.AllDirectories);

                foreach (var file in allFiles)
                {
                    var fileInfo = new FileInfo(file);
                    
                    if (fileInfo.Length > OneMB)
                    {
                        var fileSizeMB = Math.Round((double)fileInfo.Length / OneMB, 2);
                        var rowColor = DetermineRowColor(fileSizeMB);
                        
                        Files.Add(new FileItemViewModel(
                            fileName: fileInfo.Name,
                            fileSize: fileSizeMB,
                            lastModified: fileInfo.LastWriteTime,
                            rowBackground: rowColor
                        ));
                    }
                }

                Console.WriteLine($"Found {Files.Count} files larger than 1MB");
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("Access denied to some files/folders");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private string DetermineRowColor(double fileSizeMB)
        {
            // Example color logic - modify as needed
            return fileSizeMB switch
            {
                > 100 => "#FF6666",    // Red for files > 100MB
                > 50 => "#FFA500",     // Orange for files > 50MB
                > 10 => "#FFD700",     // Gold for files > 10MB
                _ => "#222222"         // Dark gray for others (1-10MB)
            };
        }
    }

    public class FileItemViewModel : ReactiveObject
    {
        private bool _isSelected;

        public string FileName { get; }
        public double FileSize { get; }
        public DateTime LastModified { get; }
        public string RowBackground { get; }
        
        public bool IsSelected
        {
            get => _isSelected;
            set => this.RaiseAndSetIfChanged(ref _isSelected, value);
        }

        public FileItemViewModel(string fileName, double fileSize, DateTime lastModified, string rowBackground)
        {
            FileName = fileName;
            FileSize = fileSize;
            LastModified = lastModified;
            RowBackground = rowBackground;
        }
    }
}