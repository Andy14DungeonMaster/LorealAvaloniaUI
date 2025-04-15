using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Serilog;

namespace LorealAvaloniaUI.ViewModels
{
    public class OneDriveViewModel : ReactiveObject
    {
        private readonly string _desktopPath;
        private readonly ConcurrentDictionary<string, string> _fileAttributes = new();
        private string _totalDesktopSize;
        private string _totalNumberOfDesktopFiles;
        private bool _selectAll;
        private bool _isSortByNameAscending = true;
        private bool _isSortBySizeAscending = true;
        private bool _isSortByDateAscending = true;

        public ObservableCollection<DesktopFileItemViewModel> DesktopFiles { get; } = new();
        public ReactiveCommand<Unit, Unit> FreeSelectedDiskSpaceCommand { get; }
        public ReactiveCommand<Unit, Unit> SortByNameCommand { get; }
        public ReactiveCommand<Unit, Unit> SortBySizeCommand { get; }
        public ReactiveCommand<Unit, Unit> SortByDateCommand { get; }

        public string TotalDesktopSize
        {
            get => _totalDesktopSize;
            set => this.RaiseAndSetIfChanged(ref _totalDesktopSize, value);
        }

        public string TotalDesktopNumber
        {
            get => _totalNumberOfDesktopFiles;
            set => this.RaiseAndSetIfChanged(ref _totalNumberOfDesktopFiles, value);
        }

        public bool SelectAll
        {
            get => _selectAll;
            set => this.RaiseAndSetIfChanged(ref _selectAll, value);
        }

        public OneDriveViewModel()
        {
            _desktopPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "OneDrive - L'Oréal\\Desktop");

            _totalDesktopSize = string.Empty;
            _totalNumberOfDesktopFiles = string.Empty;

            FreeSelectedDiskSpaceCommand = ReactiveCommand.CreateFromTask(FreeSelectedDiskSpaceAsync);
            SortByNameCommand = ReactiveCommand.Create(SortByName);
            SortBySizeCommand = ReactiveCommand.Create(SortBySize);
            SortByDateCommand = ReactiveCommand.Create(SortByDate);

            this.WhenAnyValue(x => x.SelectAll)
                .Subscribe(selectAll =>
                {
                    foreach (var file in DesktopFiles)
                    {
                        file.IsSelected = selectAll;
                    }
                });

            // Perform async initialization without blocking
            InitializeAsync().GetAwaiter().OnCompleted(() => { });
        }

        private async Task InitializeAsync()
        {
            if (!Directory.Exists(_desktopPath))
            {
                Log.Error("Desktop directory does not exist: {_desktopPath}", _desktopPath);
                await UpdateUIAsync(() => TotalDesktopSize = "Desktop directory not found.");
                return;
            }

            try
            {
                Log.Information("Initializing OneDrive Desktop Page");

                await Task.WhenAll(
                    LoadFileAttributesAsync()
                );

                await Task.WhenAll(
                    CalculateDesktopSizeAsync()  //Separated call to calculate Size on tab open
                    );
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Initialization failed");
                await UpdateUIAsync(() => TotalDesktopSize = $"Error: {ex.Message}");
            }
        }

        private async Task LoadFileAttributesAsync()
        {
            const long OneMB = 1048576;
            string command = $"attrib \"{_desktopPath}\\*.*\" /s";
            string result = await ExecuteCommandAsync(command);
            string[] lines = result.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

            var pattern = new Regex(@"\s*([^\s])\s*(C:\\.*)", RegexOptions.Compiled);
            var files = await Task.Run(() => Directory.GetFiles(_desktopPath, "*.*", SearchOption.AllDirectories));

            Parallel.ForEach(lines, line =>
            {
                var match = pattern.Match(line);
                if (match.Success)
                {
                    _fileAttributes.TryAdd(match.Groups[2].Value, match.Groups[1].Value);
                }
            });

            await Task.Run(async () =>
            {
                foreach (var file in files)
                {
                    if (_fileAttributes.TryGetValue(file, out var attr) && attr == "P")
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.Length >= OneMB)
                        {
                            var fileSizeMB = Math.Round((double)fileInfo.Length / OneMB, 2);
                            await UpdateUIAsync(() =>
                            {
                                DesktopFiles.Add(new DesktopFileItemViewModel(
                                    fileInfo.Name,
                                    fileSizeMB,
                                    fileInfo.LastWriteTime,
                                    "#222222",
                                    fileInfo.FullName));
                            });
                        }
                    }
                }
            });
        }

        private static async Task<string> ExecuteCommandAsync(string command)
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/c " + command,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                process.Start();
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                await Task.WhenAll(outputTask, errorTask);

                string output = await outputTask;
                string error = await errorTask;

                await process.WaitForExitAsync();
                return string.IsNullOrEmpty(error) ? output.Trim() : $"{output}\nStandard Error:\n{error}".Trim();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Command execution failed: {command}", command);
                return $"Error: {ex.Message}";
            }
        }

        private async Task FreeSelectedDiskSpaceAsync()
        {

            Log.Information("Delete action initiated");
            Log.Information("SIZE OF THE DISK BEFORE DELETE");
            LogSystemInformation(); // Log Size of disk before delete task

            var selectedFiles = DesktopFiles.Where(f => f.IsSelected).ToList();

            await Task.WhenAll(selectedFiles.Select(async file =>
            {
                try
                {
                    if (await Task.Run(() => File.Exists(file.FullPath)))
                    {
                        string command = $"attrib +u \"{file.FullPath}\"";
                        await ExecuteCommandAsync(command);

                        await UpdateUIAsync(() =>
                        {
                            DesktopFiles.Remove(file);
                        });

                        Log.Information("Uncached file: {fileName}", file.FileName);
                    }
                    else
                    {
                        Log.Warning("File not found: {filePath}", file.FullPath);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to uncache file: {fileName}", file.FileName);
                }
            }));

            await CalculateDesktopSizeAsync();
        }

        private async Task CalculateDesktopSizeAsync()
        {
            try
            {
                var cDrive = new DriveInfo("C");
                if (!cDrive.IsReady)
                {
                    Log.Warning("C: drive is not ready");
                    await UpdateUIAsync(() => TotalDesktopSize = "C: drive is not ready");
                    return;
                }

                long totalSize = cDrive.TotalSize;
                long freeSpace = cDrive.AvailableFreeSpace;
                double totalSizeGB = totalSize / (1024.0 * 1024.0 * 1024.0);

                Log.Information("C: Drive - Total: {totalSize:F2} GB, Free: {freeSpace:F2} GB, Used: {usedSpace:F2} GB",
                    totalSizeGB, freeSpace / (1024.0 * 1024.0 * 1024.0), (totalSize - freeSpace) / (1024.0 * 1024.0 * 1024.0));

                await UpdateUIAsync(() =>
                {
                    TotalDesktopNumber = $"Total files > 1 MB: {DesktopFiles.Count}";
                    TotalDesktopSize = $"C: Drive Size: {totalSizeGB:F2} GB";
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to calculate desktop size");
                await UpdateUIAsync(() => TotalDesktopSize = $"Error: {ex.Message}");
            }
        }

        private void SortByName()
        {
            var sorted = _isSortByNameAscending
                ? DesktopFiles.OrderBy(f => f.FileName).ToList()
                : DesktopFiles.OrderByDescending(f => f.FileName).ToList();
            UpdateDesktopFiles(sorted);
            _isSortByNameAscending = !_isSortByNameAscending;
        }

        private void SortBySize()
        {
            var sorted = _isSortBySizeAscending
                ? DesktopFiles.OrderBy(f => f.FileSize).ToList()
                : DesktopFiles.OrderByDescending(f => f.FileSize).ToList();
            UpdateDesktopFiles(sorted);
            _isSortBySizeAscending = !_isSortBySizeAscending;
        }

        private void SortByDate()
        {
            var sorted = _isSortByDateAscending
                ? DesktopFiles.OrderBy(f => f.LastModified).ToList()
                : DesktopFiles.OrderByDescending(f => f.LastModified).ToList();
            UpdateDesktopFiles(sorted);
            _isSortByDateAscending = !_isSortByDateAscending;
        }

        private void UpdateDesktopFiles(List<DesktopFileItemViewModel> sorted)
        {
            DesktopFiles.Clear();
            foreach (var file in sorted)
            {
                DesktopFiles.Add(file);
            }
        }

        private static async Task UpdateUIAsync(Action action)
        {
            try
            {
                if (Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
                {
                    // Already on UI thread, execute directly
                    action();
                }
                else
                {
                    // Post to UI thread to avoid blocking
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(action, Avalonia.Threading.DispatcherPriority.Normal);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to update UI");
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

        public class DesktopFileItemViewModel : ReactiveObject
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

            public DesktopFileItemViewModel(string fileName, double fileSize, DateTime lastModified, string rowBackground, string fullPath)
            {
                FileName = fileName;
                FileSize = fileSize;
                LastModified = lastModified;
                RowBackground = rowBackground;
                FullPath = fullPath;
            }
        }
    }
}