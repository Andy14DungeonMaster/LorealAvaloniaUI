using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
// Removed DynamicData as it was not used and caused warning
using LorealAvaloniaUI.Services; // Add this using directive
using LorealAvaloniaUI.Views;
using ReactiveUI;
using Serilog;
// Removed static System.Net.WebRequestMethods; as it was not used

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
        // REMOVE CalculateOfficeFilesCacheSizeCommand as it's now handled by the service

        // RE-INTRODUCE TotalSize property here and its backing field
        private string _totalSize;
        public string TotalSize
        {
            get => _totalSize;
            set => this.RaiseAndSetIfChanged(ref _totalSize, value);
        }

        private string _totalNumberOfFiles = "Total no. of files greater than 100 MB: 0";
        public string TotalNumber
        {
            get => _totalNumberOfFiles;
            set => this.RaiseAndSetIfChanged(ref _totalNumberOfFiles, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => this.RaiseAndSetIfChanged(ref _isLoading, value);
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

        private readonly OfficeCacheInfoService _officeCacheInfoService; // Add this field

        // MODIFY the constructor to accept OfficeCacheInfoService
        public OfficeFileCacheViewModel(OfficeCacheInfoService officeCacheInfoService)
        {
            _officeCacheInfoService = officeCacheInfoService; // Assign the injected service

            DeleteSelectedFilesCommand = ReactiveCommand.CreateFromTask(DeleteSelectedFilesAsync);
            SortFilesCommand = ReactiveCommand.Create(SortFilesByName);
            SortFilesBySizeCommand = ReactiveCommand.Create(SortFilesBySize);
            SortFilesByDateCommand = ReactiveCommand.Create(SortFilesByDate);
            // REMOVE CalculateOfficeFilesCacheSizeCommand assignment here

            DeleteSelectedFilesCommand.ThrownExceptions
                .Subscribe(ex =>
                {
                    Log.Error("Delete command error: {Ex}", ex);
                });

            // INITIALIZE TotalSize for display
            _totalSize = "Loading..."; // Set an initial loading state
            _totalNumberOfFiles = "Loading..."; // Set an initial loading state


            // Subscribe to the OfficeCacheInfoService's TotalOfficeCacheSize property
            // to update this ViewModel's TotalSize property.
            _officeCacheInfoService.WhenAnyValue(x => x.TotalOfficeCacheSize)
                .Subscribe(size => TotalSize = size);

            InitializeAsync().ConfigureAwait(false);
        }

        private async Task InitializeAsync()
        {
            await LoadFilesAsync();
            // Trigger the service to calculate the total Office cache size
            // This is important for initial load and after any operations.
            _ = _officeCacheInfoService.CalculateAndSetOfficeCacheSizeAsync();
            SetupObservables();
        }

        private void SetupObservables()
        {
            this.WhenAnyValue(x => x.Files.Count)
                .Subscribe(count => TotalNumber = $"Total no. of files greater than 100 MB: {count}");

            Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                h => Files.CollectionChanged += h,
                h => Files.CollectionChanged -= h
            )
            .StartWith(default(EventPattern<NotifyCollectionChangedEventArgs>))
            .Subscribe(_ =>
            {
                // Ensure subscriptions are properly managed if Files collection changes frequently
                if (Files.Any())
                {
                    Observable.Merge(Files.Select(f => f.WhenAnyValue(x => x.IsSelected)))
                        .Select(__ => Files.Where(f => f.IsSelected).Sum(f => f.FileSize))
                        .Subscribe(sum => SizeOfFilesSelected = $"{sum:0.00} MB");

                    Observable.Merge(Files.Select(f => f.WhenAnyValue(x => x.IsSelected)))
                        .Select(__ =>
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
                }
                else
                {
                    SizeOfFilesSelected = "0.00 MB"; // Reset if no files
                    _selectAll = false; // Reset SelectAll to false/null
                    this.RaisePropertyChanged(nameof(SelectAll));
                }
            });
        }

        private async Task LoadFilesAsync()
        {
            IsLoading = true;
            try
            {
                string officeFilesPath = Path.Combine(
                                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                                    "AppData\\Local\\Microsoft\\Office\\16.0\\OfficeFileCache\\0\\0"
                 );

                if (!Directory.Exists(officeFilesPath))
                {
                    Log.Error("Office files cache directory does not exist: {OfficeFilesCache}", officeFilesPath);
                    return;
                }

                const long OneMB = 1048576;
                var fileItems = await Task.Run(() =>
                {
                    var items = new List<OfficeFileItemViewModel>();
                    var allDirectories = Directory.GetDirectories(officeFilesPath, "*", SearchOption.AllDirectories);


                    foreach (var dir in allDirectories)
                    {
                        try
                        {
                            var dirInfo = new DirectoryInfo(dir);
                            if (!dirInfo.Exists) continue;

                            long dirSize = dirInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(f =>
                            {
                                try { return f.Length; }
                                catch (UnauthorizedAccessException) { Log.Warning("Access denied to file in dir: {File}", f.FullName); return 0; }
                                catch (FileNotFoundException) { Log.Warning("File not found in dir: {File}", f.FullName); return 0; }
                            });

                            if (dirSize > (OneMB * 100))
                            {
                                items.Add(new OfficeFileItemViewModel(
                                    fileName: dirInfo.Name,
                                    fileSize: Math.Round((double)dirSize / OneMB, 2),
                                    lastModified: dirInfo.LastWriteTime,
                                    rowBackground: DetermineRowColor(Math.Round((double)dirSize / OneMB, 2)),
                                    fullPath: dirInfo.FullName
                                ));
                            }
                        }
                        catch (UnauthorizedAccessException uae)
                        {
                            Log.Error("Access denied to directory: {Dir} - {Ex}", dir, uae.Message);
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Error processing directory {Dir}: {Ex}", dir, ex.Message);
                        }
                    }
                    return items;
                });

                Files.Clear();
                foreach (var item in fileItems)
                {
                    Files.Add(item);
                }

                Log.Information($"Found {Files.Count} Office cache directories/items larger than 100 MB.");
                Log.Information($"Office files cache path: {officeFilesPath}");
            }
            catch (Exception ex)
            {
                Log.Error($"Error loading files in OfficeFileCacheViewModel: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task DeleteSelectedFilesAsync()
        {
            Log.Information("** Delete action initiated in OfficeFileCacheViewModel **");
            Log.Information("SIZE OF THE DISK BEFORE DELETE");
            LogSystemInformation();
            IsLoading = true;
            try
            {
                var selectedFiles = Files.Where(f => f.IsSelected).ToList();
                if (!selectedFiles.Any())
                {
                    Log.Information("No files selected for deletion.");
                    return;
                }

                string message = $"Are you sure you want to permanently delete {selectedFiles.Count} item(s) from Office Cache?";
                bool confirmed = await ConfirmationDialogViewModel.ShowAsync(null, message);

                if (!confirmed)
                {
                    Log.Information("Deletion cancelled by user.");
                    return;
                }

                foreach (var file in selectedFiles)
                {
                    try
                    {
                        if (Directory.Exists(file.FullPath))
                        {
                            await Task.Run(() => Directory.Delete(file.FullPath, true));
                            Files.Remove(file);
                            Log.Information($"{file.FileName} (Office cache item) deleted successfully.");
                        }
                        else
                        {
                            Log.Information($"Office cache item not found: {file.FullPath}");
                            Files.Remove(file);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error deleting {file.FileName}: {ex.Message}");
                    }
                }
                Log.Information($"{selectedFiles.Count} item(s) processed.");
                Log.Information("SIZE OF THE DISK AFTER DELETE");
                LogSystemInformation();
                // Trigger the service to recalculate Office cache size after deletion
                _ = _officeCacheInfoService.CalculateAndSetOfficeCacheSizeAsync();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private string DetermineRowColor(double fileSizeMB)
        {
            return fileSizeMB switch
            {
                > 100 => "#FF6666",
                > 50 => "#222222",
                > 10 => "#222222",
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
                DriveInfo cDrive = new DriveInfo(@"C:\");
                long totalSize = 0;

                if (cDrive.IsReady)
                {
                    totalSize = cDrive.TotalSize;
                    long freeSpace = cDrive.AvailableFreeSpace;
                    long usedSpace = totalSize - freeSpace;

                    Log.Information("C: Drive Information - Total Space: {TotalSize} GB, Free Space: {FreeSpace} GB, Used Space: {UsedSpace} GB ", Math.Round((totalSize / (1024.0 * 1024.0 * 1024.0)), 2),
                        Math.Round((freeSpace / (1024.0 * 1024.0 * 1024.0)), 2),
                        Math.Round((usedSpace / (1024.0 * 1024.0 * 1024.0)), 2));

                }
                else
                {
                    Log.Error("C: drive is not ready.");
                }

            }
            catch (Exception ex)
            {
                Log.Error($"Error in LogSystemInformation: {ex.Message}");
            }
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