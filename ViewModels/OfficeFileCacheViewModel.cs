
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using ReactiveUI;
using static System.Net.WebRequestMethods;

namespace LorealAvaloniaUI.ViewModels
{
    public class OfficeFileCacheViewModel : ReactiveObject
    {
        public ObservableCollection<OfficeFileItemViewModel> Files { get; } = new ObservableCollection<OfficeFileItemViewModel>();

        // Command to delete files
        public ReactiveCommand<Unit, Unit> DeleteSelectedFilesCommand { get; }

        public ReactiveCommand<Unit, Unit> SortFilesCommand { get; }
        public ReactiveCommand<Unit, Unit> SortFilesBySizeCommand { get; }

        public ReactiveCommand<Unit, Unit> SortFilesByDateCommand { get; }

        public ReactiveCommand<Unit, Unit> CalculateSizeCommand { get; }

        private string _totalSize;

        public string TotalSize
        {
            get => _totalSize;
            set => this.RaiseAndSetIfChanged(ref _totalSize, value);
        }

        private string _totalNumberOfFiles;

        public string totalNumber
        {
            get => _totalNumberOfFiles;
            set => this.RaiseAndSetIfChanged(ref _totalNumberOfFiles, value);
        }

        private bool _isSortedAscending = true;
        private bool _isSizeSortedAscending = true;
        private bool _isDateSortedAscending = true;

        public OfficeFileCacheViewModel()
        {
            string OfficeFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "AppData\\Local\\Microsoft\\Office\\16.0\\OfficeFileCache\\0\\0"
            );

            OfficeFilePath.Replace("\\\\","\\");

            if (!Directory.Exists(OfficeFilePath))
            {
                Console.WriteLine("Downloads directory does not exist!");
                return;
            }

            try
            {
                const long OneMB = 1048576; // 1 MB
                var allDirectories = Directory.GetDirectories(OfficeFilePath, "*.*", SearchOption.AllDirectories);

                foreach (var file in allDirectories)
                {
                    var dirInfo = new DirectoryInfo(file);
                    long dirSize = 0;

                    if ( true )
                    {
                         dirSize += dirInfo.GetFiles().Sum(file => file.Length); 

                        var rowColor = DetermineRowColor(dirSize);

                        Files.Add(new OfficeFileItemViewModel(
                            fileName: dirInfo.Name,
                            fileSize: Math.Round((double)dirSize/OneMB,2),
                            lastModified: dirInfo.LastWriteTime,
                            rowBackground: rowColor,
                            fullPath: dirInfo.FullName
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
            SortFilesByDateCommand = ReactiveCommand.Create(SortFilesByDate);
            CalculateSizeCommand = ReactiveCommand.Create(CalculateSize);

            //Intial display Size
            CalculateSize();
        }

        private void CalculateSize()
        {



            try
            {
                // Get the Downloads directory path.  Adapt this to your needs!
                string OfficeFilePath = Path.Combine(
               Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
               "AppData\\Local\\Microsoft\\Office\\16.0\\OfficeFileCache\\0\\0"
           );

                // Check if the directory exists.
                if (Directory.Exists(OfficeFilePath))
                {
                    //Calculate total number of files.


                    totalNumber = $"Total no of folders >1 MB: {Files.Count.ToString()}";

                    // Calculate the total size of all files.
                    double totalSize = Files.Sum(f => f.FileSize);

                    // Format the size (e.g., in MB).
                    TotalSize = $"Total Size of the files: {Math.Round(totalSize,2)} MB"; // Or another formatting


                }
                else
                {
                    TotalSize = "Downloads directory not found.";
                }

            }
            catch (Exception ex)
            {
                TotalSize = $"Error: {ex.Message}"; // Handle exceptions gracefully
            }

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

        private void DeleteSelectedFiles()
        {
            var selectedFiles = Files.Where(f => f.IsSelected).ToList();

            foreach (var file in selectedFiles)
            {
                try
                {
                    Console.WriteLine($"Trying to delete: {file.FullPath}");

                    if (Directory.Exists(file.FullPath)) // Check if file exists
                    {
                        Directory.Delete(file.FullPath, true); // Delete from file system
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

            // Refresh size after delete
            CalculateSize();

            //Console.WriteLine($"{selectedFiles.Count} file(s) deleted.");
        }

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

        // Sort Files by Date (Ascending /Descending)

        private void SortFilesByDate()
        {
            var sortedFiles = _isDateSortedAscending
                ? Files.OrderBy(f => f.LastModified).ToList()
                : Files.OrderByDescending(f => f.LastModified).ToList();

            Files.Clear();
            foreach (var file in sortedFiles)
            {
                Files.Add(file);
            }

            _isDateSortedAscending = !_isDateSortedAscending; // Toggle sort order
        }

    }


    public class OfficeFileItemViewModel : ReactiveObject
    {
        private bool _isSelected;

        public string FileName { get; }
        public double FileSize { get; }
        public DateTime LastModified { get; }
        public string RowBackground { get; }
        public string FullPath { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set => this.RaiseAndSetIfChanged(ref _isSelected, value);
        }

        public OfficeFileItemViewModel(string fileName, double fileSize, DateTime lastModified, string rowBackground, string fullPath)
        {
            FileName = fileName;
            FileSize = fileSize;
            LastModified = lastModified;
            RowBackground = rowBackground;
            FullPath = fullPath;

        }
    }
}