using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using LorealAvaloniaUI.Views;
using Serilog;

namespace LorealAvaloniaUI.ViewModels
{
    public class DownloadViewModel : ReactiveObject
    {
        public ObservableCollection<FileItemViewModel> Files { get; } = new ObservableCollection<FileItemViewModel>();

        private string _selectFilesSize = "0.00 MB";
        public string SizeOfFilesSelected
        {
            get => _selectFilesSize;
            set => this.RaiseAndSetIfChanged(ref _selectFilesSize, value);
        }

        public ReactiveCommand<Unit, Unit> DeleteSelectedFilesCommand { get; }
        public ReactiveCommand<Unit, Unit> MoveSelectedFilesCommand { get; }
        public ReactiveCommand<Unit, Unit> SortFilesCommand { get; }
        public ReactiveCommand<Unit, Unit> SortFilesBySizeCommand { get; }
        public ReactiveCommand<Unit, Unit> SortFilesByDateCommand { get; }
        public ReactiveCommand<Unit, Unit> CalculateDownloadsSizeCommand { get; }

        private bool _isSortedAscending = true;
        private bool _isSizeSortedAscending = true;
        private bool _isDateSortedAscending = true;

        private string _totalDownloadsSize = "0.00 MB";
        public string TotalDownloadsSize
        {
            get => _totalDownloadsSize;
            set => this.RaiseAndSetIfChanged(ref _totalDownloadsSize, value);
        }

        private string _totalNumberOfFiles = "Total no of files > 100 MB: 0";
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

        public DownloadViewModel()
        {
            DeleteSelectedFilesCommand = ReactiveCommand.CreateFromTask(DeleteSelectedFilesAsync);
            MoveSelectedFilesCommand = ReactiveCommand.CreateFromTask(MoveSelectedFilesAsync);
            SortFilesCommand = ReactiveCommand.Create(SortFilesByName);
            SortFilesBySizeCommand = ReactiveCommand.Create(SortFilesBySize);
            SortFilesByDateCommand = ReactiveCommand.Create(SortFilesByDate);
            CalculateDownloadsSizeCommand = ReactiveCommand.CreateFromTask(CalculateDownloadsSizeAsync);

            DeleteSelectedFilesCommand.ThrownExceptions
                .Subscribe(ex =>
                {
                    Log.Error("Delete command error: {Ex}", ex);
                });

            InitializeAsync().ConfigureAwait(false);
        }

        private async Task InitializeAsync()
        {
            await LoadFilesAsync();
            await CalculateDownloadsSizeAsync();
            SetupObservables();
        }

        private void SetupObservables()
        {
            this.WhenAnyValue(x => x.Files.Count)
                .Subscribe(count => TotalNumber = $"Total no of files > 100 MB: {count}");

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
                string downloadsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads"
                );

                if (!Directory.Exists(downloadsPath))
                {
                    Log.Error("Downloads directory does not exist: {DownloadsPath}", downloadsPath);
                    return;
                }

                const long OneMB = 1048576;
                var fileItems = await Task.Run(() =>
                {
                    var items = new List<FileItemViewModel>();
                    var allFiles = Directory.GetFiles(downloadsPath, "*.*", SearchOption.AllDirectories);

                    foreach (var file in allFiles)
                    {
                        try
                        {
                            var fileInfo = new FileInfo(file);
                            if (fileInfo.Length > (OneMB * 100)) // Adding only if file is > 100 MB
                            {
                                var fileSizeMB = Math.Round((double)fileInfo.Length / OneMB, 2);
                                items.Add(new FileItemViewModel(
                                    fileInfo.Name,
                                    fileSizeMB,
                                    fileInfo.LastWriteTime,
                                    DetermineRowColor(fileSizeMB),
                                    fileInfo.FullName
                                ));
                            }
                        }
                        catch (UnauthorizedAccessException)
                        {
                            Log.Error("Access denied to file: {File}", file);
                        }
                    }
                    return items;
                });

                Files.Clear();
                foreach (var item in fileItems)
                {
                    Files.Add(item);
                }

                Log.Information($"Found {Files.Count} file(s) larger than 100 MB");
            }
            catch (Exception ex)
            {
                Log.Error($"Error loading files: {ex.Message}");
            }
            finally
            {
                IsActive = false;
            }
        }

        private async Task DeleteSelectedFilesAsync()
        {
            Log.Information("** Delete action initiated **");
            Log.Information("SIZE OF THE DISK BEFORE DELETE");
            LogSystemInformation(); // Log Size of disk before delete task
            IsActive = true;
            try
            {
                var selectedFiles = Files.Where(f => f.IsSelected).ToList();
                if (!selectedFiles.Any())
                {
                    Log.Information("No files selected for deletion.");
                    return;
                }

                string message = $"Are you sure you want to permanently delete {selectedFiles.Count} file(s)?";
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
                        if (File.Exists(file.FullPath))
                        {
                            await Task.Run(() => File.Delete(file.FullPath));
                            Files.Remove(file);
                            Log.Information("{FileName} deleted successfully.", file.FileName);
                        }
                        else
                        {
                            Log.Information("File not found: {FullPath}", file.FullPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error deleting {FileName}: {Message}", file.FileName, ex.Message);
                    }
                }
                Log.Information("{Count} file(s) processed.", selectedFiles.Count);
                Log.Information("SIZE OF THE DISK AFTER DELETE");
                LogSystemInformation(); // Log Size of disk before delete task
                await CalculateDownloadsSizeAsync();
            }
            finally
            {
                IsActive = false;
            }
        }

        private async Task MoveSelectedFilesAsync()
        {
            Log.Information("Move action initiated");
            Log.Information("SIZE OF THE DISK BEFORE MOVE");
            LogSystemInformation(); // Log Size of disk before delete task
            IsActive = true;
            try
            {
                string targetDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "OneDrive - L'Oréal\\Documents", "[LorealDiskCleanUp]MovedFilesFromDownloads"
                );
                if (!Directory.Exists(targetDirectory))
                {
                    try
                    {
                        Directory.CreateDirectory(targetDirectory);
                    }

                    catch(Exception ex)
                    {
                        Log.Error("Error creating directory {TargetDirectory}: {ex}", targetDirectory, ex);
                    }
                }

                var selectedFiles = Files.Where(f => f.IsSelected).ToList();
                foreach (var file in selectedFiles)
                {
                    try
                    {
                        if (File.Exists(file.FullPath))
                        {
                            string newPath = Path.Combine(targetDirectory, file.FileName);
                            await Task.Run(() => File.Move(file.FullPath, newPath));
                            Files.Remove(file);
                            Log.Information($"{file.FileName} moved successfully.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error moving {file.FileName}: {ex.Message}");
                    }
                }
                Log.Information("SIZE OF THE DISK AFTER MOVE");
                LogSystemInformation(); // Log Size of disk before Move task
                await CalculateDownloadsSizeAsync();
                await CalculateDownloadsSizeAsync();
            }
            finally
            {
                IsActive = false;
            }
        }

        private async Task CalculateDownloadsSizeAsync()
        {
            IsActive = true;
            try
            {
                string downloadsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads"
                );

                if (!Directory.Exists(downloadsPath))
                {
                    TotalDownloadsSize = "Downloads directory not found.";
                    return;
                }

                long totalSize = await Task.Run(() =>
                {
                    return Directory.GetFiles(downloadsPath, "*", SearchOption.AllDirectories)
                        .Sum(file =>
                        {
                            try
                            {
                                return new FileInfo(file).Length;
                            }
                            catch
                            {
                                return 0;
                            }
                        });
                });

                TotalDownloadsSize = $"Total Size of Downloads folder: {totalSize / (1024 * 1024):0.00} MB";
                TotalNumber = $"Total no of files > 100 MB: {Files.Count}";
            }
            catch (Exception ex)
            {
                TotalDownloadsSize = $"Error: {ex.Message}";
                Log.Error(TotalDownloadsSize);
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

        private void UpdateFilesCollection(IEnumerable<FileItemViewModel> sortedFiles)
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

                    Log.Information("C: Drive Information - Total Space: {TotalSize} GB, Free Space: {FreeSpace} GB, Used Space: {UsedSpace} GB ", Math.Round((totalSize / (1024.0 * 1024.0 * 1024.0)), 2), 
                        Math.Round((freeSpace / (1024.0 * 1024.0 * 1024.0)), 2),
                        Math.Round((usedSpace / (1024.0 * 1024.0 * 1024.0)), 2));
                    Log.Information("{TotalDownloadsSize}", TotalDownloadsSize);

                }
                else
                {
                    Log.Error("C: drive is not ready.");
                }

            }

            catch (Exception ex)
            {
                Log.Error($"Error: {ex.Message}");
            }
        }

    }

    public class FileItemViewModel : ReactiveObject
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

        public FileItemViewModel(string fileName, double fileSize, DateTime lastModified, string rowBackground, string fullPath)
        {
            FileName = fileName;
            FileSize = fileSize;
            LastModified = lastModified;
            RowBackground = rowBackground;
            FullPath = fullPath;
        }
    }
}