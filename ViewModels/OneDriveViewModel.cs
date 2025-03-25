
using System.Collections.ObjectModel;
using System;
using ReactiveUI;
using System.IO;

namespace LorealAvaloniaUI.ViewModels
{
    public class OneDriveViewModel : ReactiveObject
    {
        public ObservableCollection<DesktopFileItemViewModel> DesktopFiles { get; }

        public OneDriveViewModel()
        {
            DesktopFiles = new ObservableCollection<DesktopFileItemViewModel>
            {
                new DesktopFileItemViewModel("Report.pdf", 750, DateTime.Now.AddDays(-2), "#222222"),
                new DesktopFileItemViewModel("Video.mp4", 1024, DateTime.Now.AddDays(-5), "#333333"),
                new DesktopFileItemViewModel("Music.mp3", 600, DateTime.Now.AddDays(-1), "#222222")
            };
        }
        
        /*
        public OneDriveViewModel()
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
                const long OneMB = 1048576; // 1 MB
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
                            rowBackground: rowColor,
                            fullPath: fileInfo.FullName
                        ));
                    }
                }

                Console.WriteLine($"Found {Files.Count} files larger than 1MB");
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("Access denied to some files/folders.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            // ✅ Initialize commands
            DeleteSelectedFilesCommand = ReactiveCommand.Create(DeleteSelectedFiles);
            SortFilesCommand = ReactiveCommand.Create(SortFilesByName);
            SortFilesBySizeCommand = ReactiveCommand.Create(SortFilesBySize);
        }

        private string DetermineRowColor(double fileSizeMB)
        {
            // Define color logic based on file size
            return fileSizeMB switch
            {
                > 100 => "#FF6666",    // Red for files > 100MB
                > 50 => "#FFA500",     // Orange for files > 50MB
                > 10 => "#FFD700",     // Gold for files > 10MB
                _ => "#222222"         // Dark gray for others (1-10MB)
            };
        }

        private void MoveSelectedFiles()
        {
            var selectedFiles = Files.Where(f => f.IsSelected).ToList();

            foreach (var file in selectedFiles)
            {
                try
                {
                    Console.WriteLine($"Trying to delete: {file.FullPath}");

                    if (File.Exists(file.FullPath)) // Check if file exists
                    {
                        File.Delete(file.FullPath); // Delete from file system
                        Files.Remove(file);         // Remove from UI
                        Console.WriteLine($"{file.FileName} deleted successfully.");
                    }
                    else
                    {
                        Console.WriteLine($"File not found: {file.FullPath}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting file: {ex.Message}");
                }
            }

            Console.WriteLine($"{selectedFiles.Count} file(s) deleted.");
        }

        private void SizeDetermination()
        {
            var selectedFiles = Files.Where(f => f.IsSelected).ToList();

            foreach (var file in selectedFiles)
            {
                try
                {
                    Console.WriteLine($"Trying to delete: {file.FullPath}");

                    if (File.Exists(file.FullPath)) // Check if file exists
                    {
                        File.Delete(file.FullPath); // Delete from file system
                        Files.Remove(file);         // Remove from UI
                        Console.WriteLine($"{file.FileName} deleted successfully.");
                    }
                    else
                    {
                        Console.WriteLine($"File not found: {file.FullPath}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting file: {ex.Message}");
                }
            }

            Console.WriteLine($"{selectedFiles.Count} file(s) deleted.");
        }



        private void DeleteSelectedFiles()
        {
            var selectedFiles = Files.Where(f => f.IsSelected).ToList();

            foreach (var file in selectedFiles)
            {
                try
                {
                    Console.WriteLine($"Trying to delete: {file.FullPath}");

                    if (File.Exists(file.FullPath)) // Check if file exists
                    {
                        File.Delete(file.FullPath); // Delete from file system
                        Files.Remove(file);         // Remove from UI
                        Console.WriteLine($"{file.FileName} deleted successfully.");
                    }
                    else
                    {
                        Console.WriteLine($"File not found: {file.FullPath}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting file: {ex.Message}");
                }
            }

            Console.WriteLine($"{selectedFiles.Count} file(s) deleted.");
        }

        //  Sort Files by Name (Ascending/Descending)
        private void SortFilesByName()
        {
            var sortedFiles = _isSortedAscending
                ? Files.OrderBy(f => f.FileName).ToList()
                : Files.OrderByDescending(f => f.FileName).ToList();

            Files.Clear();
            foreach (var file in sortedFiles)
            {
                Files.Add(file);
            }

            _isSortedAscending = !_isSortedAscending; // Toggle sort order
        }

        //  Sort Files by Size (Ascending/Descending)
        private void SortFilesBySize()
        {
            var sortedFiles = _isSizeSortedAscending
                ? Files.OrderBy(f => f.FileSize).ToList()
                : Files.OrderByDescending(f => f.FileSize).ToList();

            Files.Clear();
            foreach (var file in sortedFiles)
            {
                Files.Add(file);
            }

            _isSizeSortedAscending = !_isSizeSortedAscending; // Toggle sort order
        }
    }
        */

    public class DesktopFileItemViewModel : ReactiveObject
        {
            public string FileName { get; }
            public double FileSize { get; }
            public DateTime LastModified { get; }
            public string RowBackground { get; }
            public bool IsSelected { get; set; }

            public DesktopFileItemViewModel(string fileName, double fileSize, DateTime lastModified, string rowBackground)
            {
                FileName = fileName;
                FileSize = fileSize;
                LastModified = lastModified;
                RowBackground = rowBackground;
            }
        }
    }
}