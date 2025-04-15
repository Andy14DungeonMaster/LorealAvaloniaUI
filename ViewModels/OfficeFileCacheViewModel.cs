
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using LorealAvaloniaUI.Views;
using ReactiveUI;
using Serilog;
using static System.Net.WebRequestMethods;

namespace LorealAvaloniaUI.ViewModels
{
    public class OfficeFileCacheViewModel : ReactiveObject
    {
        public ObservableCollection<OfficeFileItemViewModel> Files { get; } = new ObservableCollection<OfficeFileItemViewModel>();

        private string _selectFilesSize = "0.00 MB";
        public string SizeOfFilesSelected
        {
            get => _selectFilesSize;
            set => this.RaiseAndSetIfChanged(ref _selectFilesSize, value);
        }
        // Command to delete files
        public ReactiveCommand<Unit, Unit> DeleteSelectedFilesCommand { get; }

        public ReactiveCommand<Unit, Unit> SortFilesCommand { get; }
        public ReactiveCommand<Unit, Unit> SortFilesBySizeCommand { get; }
        public ReactiveCommand<Unit, Unit> SortFilesByDateCommand { get; }
        public ReactiveCommand<Unit, Unit> CalculateOfficeFilesCacheSizeCommand { get; }

        private string _totalSize;

        public string TotalSize
        {
            get => _totalSize;
            set => this.RaiseAndSetIfChanged(ref _totalSize, value);
        }

        private string _totalNumberOfFiles = "Total no of files > 1 MB: 0";
        public string TotalNumber
        {
            get => _totalNumberOfFiles;
            set => this.RaiseAndSetIfChanged(ref _totalNumberOfFiles, value);
        }

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set => this.RaiseAndSetIfChanged(ref _isActive, value);
        }

        private bool _isSortedAscending = true;
        private bool _isSizeSortedAscending = true;
        private bool _isDateSortedAscending = true;

        private bool? _selectAll = false;
        public bool? SelectAll
        {
            get => _selectAll;
            set
            {
                var oldValue = _selectAll;
                this.RaiseAndSetIfChanged(ref _selectAll, value);
                if (_selectAll != oldValue && value.HasValue)
                {
                    foreach (var file in Files)
                    {
                        file.IsSelected = value.Value;
                    }
                }
            }
        }
        public OfficeFileCacheViewModel()
        {
            DeleteSelectedFilesCommand = ReactiveCommand.CreateFromTask(DeleteSelectedFilesAsync);
            SortFilesCommand = ReactiveCommand.Create(SortFilesByName);
            SortFilesBySizeCommand = ReactiveCommand.Create(SortFilesBySize);
            SortFilesByDateCommand = ReactiveCommand.Create(SortFilesByDate);
            CalculateOfficeFilesCacheSizeCommand = ReactiveCommand.CreateFromTask(CalculateOfficeFilesCacheSizeAsync);

            DeleteSelectedFilesCommand.ThrownExceptions
               .Subscribe(ex =>
               {
                   Console.WriteLine($"Delete command error: {ex}");
               });

            InitializeAsync().ConfigureAwait(false);

            //string officeFilesPath = Path.Combine(
            //    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            //    "AppData\\Local\\Microsoft\\Office\\16.0\\OfficeFileCache\\0\\0"
            //);

        }

        private async Task InitializeAsync()
        {
            await LoadFilesAsync();
            await CalculateOfficeFilesCacheSizeAsync();
            SetupObservables();
        }

        private void SetupObservables()
        {
            this.WhenAnyValue(x => x.Files.Count)
                .Subscribe(count => TotalNumber = $"Total no of files > 1 MB: {count}");

            Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                h => Files.CollectionChanged += h,
                h => Files.CollectionChanged -= h
            )
            .StartWith(default(EventPattern<NotifyCollectionChangedEventArgs>))
            .Subscribe(_ =>
            {
                Observable.Merge(Files.Select(f => f.WhenAnyValue(x => x.IsSelected)))
                    .Select(_ => Files.Where(f => f.IsSelected).Sum(f => f.FileSize))
                    .Subscribe(sum => SizeOfFilesSelected = $"{sum:0.00} MB");

                Observable.Merge(Files.Select(f => f.WhenAnyValue(x => x.IsSelected)))
                    .Select(_ =>
                    {
                        if (Files.Count == 0) return false;
                        bool allSelected = Files.All(f => f.IsSelected);
                        bool noneSelected = Files.All(f => !f.IsSelected);
                        return allSelected ? true : noneSelected ? false : (bool?)null;
                    })
                    .Subscribe(state =>
                    {
                        if (_selectAll != state)
                        {
                            _selectAll = state;
                            this.RaisePropertyChanged(nameof(SelectAll));
                        }
                    });
            });
        }

        private async Task LoadFilesAsync()
        {
            IsActive = true;
            try
            {
                string officeFilesPath = Path.Combine(
                                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                                    "AppData\\Local\\Microsoft\\Office\\16.0\\OfficeFileCache\\0\\0"
                 );

                if (!Directory.Exists(officeFilesPath))
                {
                    Console.WriteLine("Downloads directory does not exist!");
                    return;
                }

                const long OneMB = 1048576;
                var fileItems = await Task.Run(() =>
                {
                    var items = new List<OfficeFileItemViewModel>();
                    var allDirectories = Directory.GetDirectories(officeFilesPath, "*.*", SearchOption.AllDirectories);


                    foreach (var file in allDirectories)
                    {
                        try
                        {
                            var dirInfo = new DirectoryInfo(file);
                            long dirSize = 0;
                            dirSize += dirInfo.GetFiles().Sum(file => file.Length);

                            if (dirSize > OneMB)
                            {
                                                               items.Add(new OfficeFileItemViewModel(
                                    fileName: dirInfo.Name,
                                    fileSize: Math.Round((double)dirSize / OneMB, 2),
                                    lastModified: dirInfo.LastWriteTime,
                                    rowBackground: "#222222",
                                    fullPath: dirInfo.FullName
                                ));
                            }
                        }
                        catch (UnauthorizedAccessException)
                        {
                            Console.WriteLine($"Access denied to file: {file}");
                        }
                    }
                    return items;
                });

                Files.Clear();
                foreach (var item in fileItems)
                {
                    Files.Add(item);
                }

                Console.WriteLine($"Found {Files.Count} files larger than 1MB");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading files: {ex.Message}");
            }
            finally
            {
                IsActive = false;
            }
        }

        private async Task DeleteSelectedFilesAsync()
        {
            Log.Information("Delete action initiated");
            Log.Information("SIZE OF THE DISK BEFORE DELETE");
            LogSystemInformation(); // Log Size of disk before delete task
            IsActive = true;
            try
            {
                var selectedFiles = Files.Where(f => f.IsSelected).ToList();
                if (!selectedFiles.Any())
                {
                    Console.WriteLine("No files selected for deletion.");
                    return;
                }

                string message = $"Are you sure you want to permanently delete {selectedFiles.Count} file(s)?";
                bool confirmed = await ConfirmationDialogViewModel.ShowAsync(null, message);

                if (!confirmed)
                {
                    Console.WriteLine("Deletion cancelled by user.");
                    return;
                }

                foreach (var file in selectedFiles)
                {
                    try
                    {
                        if (Directory.Exists(file.FullPath)) // Check if the directory exists
                        {
                            await Task.Run(() => Directory.Delete(file.FullPath, true)); // 
                            Files.Remove(file);
                            Console.WriteLine($"{file.FileName} deleted successfully.");
                            Log.Information($"{file.FileName} deleted successfully.");
                        }
                        else
                        {
                            Console.WriteLine($"File not found: {file.FullPath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting {file.FileName}: {ex.Message}");
                    }
                }
                Console.WriteLine($"{selectedFiles.Count} file(s) processed.");
                Log.Information("SIZE OF THE DISK AFTER DELETE");
                LogSystemInformation(); // Log Size of disk before delete task
                await CalculateOfficeFilesCacheSizeAsync();
            }
            finally
            {
                IsActive = false;
            }
        }

        private async Task CalculateOfficeFilesCacheSizeAsync()
        {
            IsActive = true;
            try
            {
                string officeFilesPath = Path.Combine(
                                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                                    "AppData\\Local\\Microsoft\\Office\\16.0\\OfficeFileCache\\0\\0"
                 );

                if (!Directory.Exists(officeFilesPath))
                {
                    TotalSize = "Office Files Cache directory not found.";
                    return;
                }


                double totalSize = await Task.Run(() =>
                {

                    //Calculate the total size of all files.
                    DirectoryInfo dirInfo = new DirectoryInfo(officeFilesPath);
                    return dirInfo.GetFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);
                    //return Directory.GetDirectories(officeFilesPath, "*", SearchOption.AllDirectories)
                    //    .Sum(file =>
                    //    {
                    //        try
                    //        {
                    //            return new DirectoryInfo(file).Length;
                    //        }
                    //        catch
                    //        {
                    //            return 0;
                    //        }
                    //    });
                });

                TotalSize = $"Total Size: {totalSize / (1024 * 1024):0.00} MB";
                TotalNumber = $"Total no of files > 1 MB: {Files.Count}";
                //Log.Information("Downloads: ");
                //Log.Information(TotalDownloadsSize);
                //Log.Information(TotalNumber);
            }
            catch (Exception ex)
            {
                TotalSize = $"Error: {ex.Message}";
            }
            finally
            {
                IsActive = false;
            }
        }

        private string DetermineRowColor(double fileSizeMB)
        {
            return fileSizeMB switch
            {
                > 100 => "#FF6666",
                > 50 => "#FFA500",
                > 10 => "#FFD700",
                _ => "#222222"
            };
        }

        private void SortFilesByName()
        {
            var sortedFiles = _isSortedAscending
                ? Files.OrderBy(f => f.FileName).ToList()
                : Files.OrderByDescending(f => f.FileName).ToList();

            UpdateFilesCollection(sortedFiles);
            _isSortedAscending = !_isSortedAscending;
        }

        private void SortFilesBySize()
        {
            var sortedFiles = _isSizeSortedAscending
                ? Files.OrderBy(f => f.FileSize).ToList()
                : Files.OrderByDescending(f => f.FileSize).ToList();

            UpdateFilesCollection(sortedFiles);
            _isSizeSortedAscending = !_isSizeSortedAscending;
        }

        private void SortFilesByDate()
        {
            var sortedFiles = _isDateSortedAscending
                ? Files.OrderBy(f => f.LastModified).ToList()
                : Files.OrderByDescending(f => f.LastModified).ToList();

            UpdateFilesCollection(sortedFiles);
            _isDateSortedAscending = !_isDateSortedAscending;
        }

        private void UpdateFilesCollection(IEnumerable<OfficeFileItemViewModel> sortedFiles)
        {
            Files.Clear();
            foreach (var file in sortedFiles)
            {
                Files.Add(file);
            }
        }

        private void LogSystemInformation()
        {
            try
            {
                // Get the Desktop directory path.  Adapt this to your needs!
                DriveInfo cDrive = new DriveInfo(@"C:\");
                long totalSize = 0;

                if (cDrive.IsReady)
                {
                    // Total size of the drive in bytes
                    totalSize = cDrive.TotalSize;

                    // Available free space in bytes
                    long freeSpace = cDrive.AvailableFreeSpace;

                    // Used space in bytes
                    long usedSpace = totalSize - freeSpace;

                    Log.Information("C: Drive Information:");
                    Log.Information("Total Size: " + Math.Round((totalSize / (1024.0 * 1024.0 * 1024.0)), 2) + " GB");
                    Log.Information("Free Space:" + Math.Round((freeSpace / (1024.0 * 1024.0 * 1024.0)), 2) + " GB");
                    Log.Information("Used Space:" + Math.Round((usedSpace / (1024.0 * 1024.0 * 1024.0)), 2) + " GB");

                }
                else
                {
                    Console.WriteLine("C: drive is not ready.");
                }

            }

            catch (Exception ex)
            {
                Log.Information($"Error: {ex.Message}");
            }
        }

    }

    /*
    private void CalculateSize()
    {



        try
        {
            // Get the Downloads directory path.  Adapt this to your needs!
            string officeFilesPath = Path.Combine(
           Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
           "AppData\\Local\\Microsoft\\Office\\16.0\\OfficeFileCache\\0\\0"
       );

            // Check if the directory exists.
            if (Directory.Exists(officeFilesPath))
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
    */




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
        