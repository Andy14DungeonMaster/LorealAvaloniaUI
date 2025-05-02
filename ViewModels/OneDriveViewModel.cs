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
using System.Management.Automation;
using Serilog;
using LorealAvaloniaUI.Views;
using LorealAvaloniaUI.Services;

namespace LorealAvaloniaUI.ViewModels
{
    public class OneDriveViewModel : ReactiveObject
    {
        public enum TabType
        {
            Desktop,
            Documents,
            Pictures
        }

        // Paths
        private readonly string desktopPath;
        private readonly string documentsPath;
        private readonly string picturesPath;
        private readonly ConcurrentDictionary<string, string> _fileAttributes = new();

        // Desktop Tab
        private string _totalDesktopSize;
        private string _totalNumberOfDesktopFiles;
        private bool _selectAllDesktop;
        private bool _isSortByNameAscendingDesktop = true;
        private bool _isSortBySizeAscendingDesktop = true;
        private bool _isSortByDateAscendingDesktop = true;

        // Documents Tab
        private string _totalDocumentsSize;
        private string _totalNumberOfDocumentsFiles;
        private bool _selectAllDocuments;

        private bool _isSortByNameAscendingDocuments = true;
        private bool _isSortBySizeAscendingDocuments = true;
        private bool _isSortByDateAscendingDocuments = true;

        // Pictures Tab
        private string _totalPicturesSize;
        private string _totalNumberOfPicturesFiles;
        private bool _selectAllPictures;
        private bool _isSortByNameAscendingPictures = true;
        private bool _isSortBySizeAscendingPictures = true;
        private bool _isSortByDateAscendingPictures = true;

        // Tab Names
        private readonly string _desktopTab = TabType.Desktop.ToString();
        private readonly string _documentsTab = TabType.Documents.ToString();
        private readonly string _picturesTab = TabType.Pictures.ToString();

        // OneDrive Path
        private readonly string oneDrivePath = Environment.GetEnvironmentVariable("OneDrive");

        // File Collections
        public ObservableCollection<DesktopFileItemViewModel> DesktopFiles { get; } = new();
        public ObservableCollection<DesktopFileItemViewModel> DocumentFiles { get; } = new();
        public ObservableCollection<DesktopFileItemViewModel> PicturesFiles { get; } = new();

        // Commands
        public ReactiveCommand<Unit, Unit> FreeSelectedDiskSpaceCommand { get; }
        public ReactiveCommand<TabType, Unit> SortByNameCommand { get; }
        public ReactiveCommand<TabType, Unit> SortBySizeCommand { get; }
        public ReactiveCommand<TabType, Unit> SortByDateCommand { get; }
        public ReactiveCommand<Unit, Unit> FreeSelectedDiskSpaceCommandDocuments { get; }
        public ReactiveCommand<Unit, Unit> FreeSelectedDiskSpaceCommandPictures { get; }
        public ReactiveCommand<Unit, Unit> FreeAllDiskSpaceCommand { get; }

        // Properties
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

        public string TotalDocumentSize
        {
            get => _totalDocumentsSize;
            set => this.RaiseAndSetIfChanged(ref _totalDocumentsSize, value);
        }

        public string TotalDocumentNumber
        {
            get => _totalNumberOfDocumentsFiles;
            set => this.RaiseAndSetIfChanged(ref _totalNumberOfDocumentsFiles, value);
        }

        public string TotalPicturesSize
        {
            get => _totalPicturesSize;
            set => this.RaiseAndSetIfChanged(ref _totalPicturesSize, value);
        }

        public string TotalPicturesNumber
        {
            get => _totalNumberOfPicturesFiles;
            set => this.RaiseAndSetIfChanged(ref _totalNumberOfPicturesFiles, value);
        }

        public bool SelectAllDesktop
        {
            get => _selectAllDesktop;
            set => this.RaiseAndSetIfChanged(ref _selectAllDesktop, value);
        }

        public bool SelectAllDocuments
        {
            get => _selectAllDocuments;
            set => this.RaiseAndSetIfChanged(ref _selectAllDocuments, value);
        }

        public bool SelectAllPictures
        {
            get => _selectAllPictures;
            set => this.RaiseAndSetIfChanged(ref _selectAllPictures, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => this.RaiseAndSetIfChanged(ref _isLoading, value);
        }

        public OneDriveViewModel()
        {
            desktopPath = Path.Combine(oneDrivePath, "Desktop");
            documentsPath = Path.Combine(oneDrivePath, "Documents");
            picturesPath = Path.Combine(oneDrivePath, "Pictures");

            _totalDesktopSize = string.Empty;
            _totalNumberOfDesktopFiles = string.Empty;
            _totalDocumentsSize = string.Empty;
            _totalNumberOfDocumentsFiles = string.Empty;
            _totalPicturesSize = string.Empty;
            _totalNumberOfPicturesFiles = string.Empty;

            // Initialize Commands
            FreeSelectedDiskSpaceCommand = ReactiveCommand.CreateFromTask(FreeSelectedDiskSpaceAsync);
            SortByNameCommand = ReactiveCommand.Create<TabType>(tab => SortByName(tab.ToString()));
            SortBySizeCommand = ReactiveCommand.Create<TabType>(tab => SortBySize(tab.ToString()));
            SortByDateCommand = ReactiveCommand.Create<TabType>(tab => SortByDate(tab.ToString()));
            FreeSelectedDiskSpaceCommandDocuments = ReactiveCommand.CreateFromTask(FreeSelectedDiskSpaceDocumentsAsync);
            FreeSelectedDiskSpaceCommandPictures = ReactiveCommand.CreateFromTask(FreeSelectedDiskSpacePicturesAsync);
            FreeAllDiskSpaceCommand = ReactiveCommand.CreateFromTask(FreeAllDiskSpaceAsync);

            // Subscribe to SelectAll Properties
            this.WhenAnyValue(x => x.SelectAllDesktop)
                .Subscribe(selectAll =>
                {
                    foreach (var file in DesktopFiles)
                    {
                        file.IsSelected = selectAll;
                    }
                });

            this.WhenAnyValue(x => x.SelectAllDocuments)
                .Subscribe(selectAll =>
                {
                    foreach (var file in DocumentFiles)
                    {
                        file.IsSelected = selectAll;
                    }
                });

            this.WhenAnyValue(x => x.SelectAllPictures)
                .Subscribe(selectAll =>
                {
                    foreach (var file in PicturesFiles)
                    {
                        file.IsSelected = selectAll;
                    }
                });

            // Perform async initialization
            InitializeAsync().GetAwaiter().OnCompleted(() => { });
        }

        private async Task InitializeAsync()
        {
            bool desktopExists = Directory.Exists(desktopPath);
            bool documentsExists = Directory.Exists(documentsPath);
            bool picturesExists = Directory.Exists(picturesPath);

            if (!desktopExists)
            {
                Log.Error("Desktop directory does not exist: {DesktopPath}", desktopPath);
                await UpdateUIAsync(() => TotalDesktopSize = "Desktop directory not found.");
            }

            if (!documentsExists)
            {
                Log.Error("Documents directory does not exist: {DocumentsPath}", documentsPath);
                await UpdateUIAsync(() => TotalDocumentSize = "Documents directory not found.");
            }

            if (!picturesExists)
            {
                Log.Error("Pictures directory does not exist: {PicturesPath}", picturesPath);
                await UpdateUIAsync(() => TotalPicturesSize = "Pictures directory not found.");
            }

            if (!desktopExists || !documentsExists || !picturesExists)
            {
                return;
            }

            try
            {
                await Task.WhenAll(
                    LoadFileAttributesAsync(desktopPath, DesktopFiles, _desktopTab),
                    LoadFileAttributesAsync(documentsPath, DocumentFiles, _documentsTab),
                    LoadFileAttributesAsync(picturesPath, PicturesFiles, _picturesTab)
                );

                Log.Information("Found {Count} cached desktop file(s) larger than 100 MB", DesktopFiles.Count);
                Log.Information("Found {Count} cached documents file(s) larger than 100 MB", DocumentFiles.Count);
                Log.Information("Found {Count} cached pictures file(s) larger than 100 MB", PicturesFiles.Count);

                await CalculateSizeAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Initialization failed");
                await UpdateUIAsync(() =>
                {
                    TotalDesktopSize = $"Error: {ex.Message}";
                    TotalDocumentSize = $"Error: {ex.Message}";
                    TotalPicturesSize = $"Error: {ex.Message}";
                });
            }
        }

        private async Task LoadFileAttributesAsync(string path, ObservableCollection<DesktopFileItemViewModel> targetCollection, string tabName)
        {
            IsLoading = true;
            const long OneMB = 1048576;
            string command = $"attrib \"{path}\\*.*\" /s";
            string result = await ExecuteCommandAsync(command);
            string[] lines = result.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

            var pattern = new Regex(@"\s*([^\s])\s*(C:\\.*)", RegexOptions.Compiled);
            var files = await Task.Run(() => Directory.GetFiles(path, "*.*", SearchOption.AllDirectories));

            Parallel.ForEach(lines, line =>
            {
                var match = pattern.Match(line);
                if (match.Success)
                {
                    _fileAttributes.TryAdd(match.Groups[2].Value, match.Groups[1].Value);
                }
            });

            var tempItems = new List<DesktopFileItemViewModel>();

            try
            {
                await Task.Run(() =>
                {
                    foreach (var file in files)
                    {
                        if (_fileAttributes.TryGetValue(file, out var attr) && attr == "P")
                        {
                            var fileInfo = new FileInfo(file);
                            if (fileInfo.Length >= (OneMB * 100))
                            {
                                var fileSizeMB = Math.Round((double)fileInfo.Length / OneMB, 2);
                                tempItems.Add(new DesktopFileItemViewModel(
                                    fileInfo.Name,
                                    fileSizeMB,
                                    fileInfo.LastWriteTime,
                                    "#222222",
                                    fileInfo.FullName));
                            }
                        }
                    }
                });

                await UpdateUIAsync(() =>
                {
                    foreach (var item in tempItems)
                    {
                        targetCollection.Add(item);
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Error($"Error loading files for {tabName}: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
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
            await FreeDiskSpaceAsync(DesktopFiles, _desktopTab);
        }

        private async Task FreeSelectedDiskSpaceDocumentsAsync()
        {
            await FreeDiskSpaceAsync(DocumentFiles, _documentsTab);
        }

        private async Task FreeSelectedDiskSpacePicturesAsync()
        {
            await FreeDiskSpaceAsync(PicturesFiles, _picturesTab);
        }

        private async Task FreeDiskSpaceAsync(ObservableCollection<DesktopFileItemViewModel> files, string tabSelected)
        {
            IsLoading = true;

            Log.Information("** Uncache action initiated **");
            Log.Information("SIZE OF THE DISK BEFORE DELETE");
            LogSystemInformation();

            var selectedFiles = files.Where(f => f.IsSelected).ToList();

            await Task.WhenAll(selectedFiles.Select(async file =>
            {
                try
                {
                    if (await Task.Run(() => File.Exists(file.FullPath)))
                    {
                        string command = $"attrib +u -p \"{file.FullPath}\"";
                        await ExecuteCommandAsync(command);

                        FileDeletionTracker.Instance.LogUncaching(file.FileName, file.FullPath, file.FileSize);

                        await UpdateUIAsync(() =>
                        {
                            if (tabSelected == _desktopTab)
                            {
                                DesktopFiles.Remove(file);
                            }
                            else if (tabSelected == _documentsTab)
                            {
                                DocumentFiles.Remove(file);
                            }
                            else if (tabSelected == _picturesTab)
                            {
                                PicturesFiles.Remove(file);
                            }
                            else
                            {
                                Log.Information("File not removed from UI {Filepath}", file.FullPath);
                            }
                        });

                        Log.Information("{FileName} uncached successfully.", file.FullPath);
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

            Log.Information("{Count} file(s) processed.", selectedFiles.Count);
            Log.Information("SIZE OF THE DISK AFTER DELETE");
            LogSystemInformation();
            await CalculateSizeAsync();

            IsLoading = false;
        }

        private async Task FreeAllDiskSpaceAsync()
        {
            IsLoading = true;

            Log.Information("** Free up all space initiated **");
            Log.Information("SIZE OF THE DISK BEFORE");
            LogSystemInformation();

            try
            {
                using var ps = PowerShell.Create();

                string message = $"This saves space on this PC by setting all your files to online-only, including the files that are currently set to \"Always keep on this device\". The first time you open a file in the future, you'll need to be online.";
                bool confirmed = await ConfirmationDialogViewModel.ShowAsync(null, message);

                if (!confirmed)
                {
                    Log.Information("Free up all space cancelled by user.");
                    return;
                }

                ps.AddScript(@"get-childitem $ENV:OneDriveCommercial -Force -File -Recurse -ErrorAction SilentlyContinue | Where-Object {$_.Attributes -match 'ReparsePoint' -or $_.Attributes -eq '525344' } | ForEach-Object { attrib.exe $_.fullname +U -P /s }");
                var result = await ps.InvokeAsync();

                Log.Information($"All the files on this device are set to online-only");
                Log.Information("SIZE OF THE DISK AFTER");
                LogSystemInformation();

                if (result != null)
                {
                    Log.Information(result.ToString());
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to execute unpinning");
            }
            finally
            {
                IsLoading = false;
            }

            await CalculateSizeAsync();
        }

        private async Task CalculateSizeAsync()
        {
            IsLoading = true;
            try
            {
                var cDrive = new DriveInfo("C");
                if (!cDrive.IsReady)
                {
                    Log.Warning("C: drive is not ready");
                    await UpdateUIAsync(() =>
                    {
                        TotalDesktopSize = "C: drive is not ready";
                        TotalDocumentSize = "C: drive is not ready";
                        TotalPicturesSize = "C: drive is not ready";
                    });
                    return;
                }

                double totalSizeGB = cDrive.TotalSize / (1024.0 * 1024.0 * 1024.0);

                await UpdateUIAsync(() =>
                {
                    TotalDesktopNumber = $"Total no. of files greater than 100 MB: {DesktopFiles.Count}";
                    TotalDesktopSize = $"Size of C:\\ drive: {totalSizeGB:F2} GB";

                    TotalDocumentNumber = $"Total no. of files greater than 100 MB: {DocumentFiles.Count}";
                    TotalDocumentSize = $"Size of C:\\ drive: {totalSizeGB:F2} GB";

                    TotalPicturesNumber = $"Total no. of files greater than 100 MB: {PicturesFiles.Count}";
                    TotalPicturesSize = $"Size of C:\\ drive: {totalSizeGB:F2} GB";
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to calculate drive size");
                await UpdateUIAsync(() =>
                {
                    TotalDesktopSize = $"Error: {ex.Message}";
                    TotalDocumentSize = $"Error: {ex.Message}";
                    TotalPicturesSize = $"Error: {ex.Message}";
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void SortByName(string tabSelected)
        {
            if (tabSelected == _desktopTab)
            {
                var sorted = _isSortByNameAscendingDesktop
                    ? DesktopFiles.OrderBy(f => f.FileName).ToList()
                    : DesktopFiles.OrderByDescending(f => f.FileName).ToList();
                UpdateFiles(sorted, _desktopTab);
                _isSortByNameAscendingDesktop = !_isSortByNameAscendingDesktop;
            }
            else if (tabSelected == _documentsTab)
            {
                var sorted = _isSortByNameAscendingDocuments
                    ? DocumentFiles.OrderBy(f => f.FileName).ToList()
                    : DocumentFiles.OrderByDescending(f => f.FileName).ToList();
                UpdateFiles(sorted, _documentsTab);
                _isSortByNameAscendingDocuments = !_isSortByNameAscendingDocuments;
            }
            else if (tabSelected == _picturesTab)
            {
                var sorted = _isSortByNameAscendingPictures
                    ? PicturesFiles.OrderBy(f => f.FileName).ToList()
                    : PicturesFiles.OrderByDescending(f => f.FileName).ToList();
                UpdateFiles(sorted, _picturesTab);
                _isSortByNameAscendingPictures = !_isSortByNameAscendingPictures;
            }
        }

        private void SortBySize(string tabSelected)
        {
            if (tabSelected == _desktopTab)
            {
                var sorted = _isSortBySizeAscendingDesktop
                    ? DesktopFiles.OrderBy(f => f.FileSize).ToList()
                    : DesktopFiles.OrderByDescending(f => f.FileSize).ToList();
                UpdateFiles(sorted, _desktopTab);
                _isSortBySizeAscendingDesktop = !_isSortBySizeAscendingDesktop;
            }
            else if (tabSelected == _documentsTab)
            {
                var sorted = _isSortBySizeAscendingDocuments
                    ? DocumentFiles.OrderBy(f => f.FileSize).ToList()
                    : DocumentFiles.OrderByDescending(f => f.FileSize).ToList();
                UpdateFiles(sorted, _documentsTab);
                _isSortBySizeAscendingDocuments = !_isSortBySizeAscendingDocuments;
            }
            else if (tabSelected == _picturesTab)
            {
                var sorted = _isSortBySizeAscendingPictures
                    ? PicturesFiles.OrderBy(f => f.FileSize).ToList()
                    : PicturesFiles.OrderByDescending(f => f.FileSize).ToList();
                UpdateFiles(sorted, _picturesTab);
                _isSortBySizeAscendingPictures = !_isSortBySizeAscendingPictures;
            }
        }

        private void SortByDate(string tabSelected)
        {
            if (tabSelected == _desktopTab)
            {
                var sorted = _isSortByDateAscendingDesktop
                    ? DesktopFiles.OrderBy(f => f.LastModified).ToList()
                    : DesktopFiles.OrderByDescending(f => f.LastModified).ToList();
                UpdateFiles(sorted, _desktopTab);
                _isSortByDateAscendingDesktop = !_isSortByDateAscendingDesktop;
            }
            else if (tabSelected == _documentsTab)
            {
                var sorted = _isSortByDateAscendingDocuments
                    ? DocumentFiles.OrderBy(f => f.LastModified).ToList()
                    : DocumentFiles.OrderByDescending(f => f.LastModified).ToList();
                UpdateFiles(sorted, _documentsTab);
                _isSortByDateAscendingDocuments = !_isSortByDateAscendingDocuments;
            }
            else if (tabSelected == _picturesTab)
            {
                var sorted = _isSortByDateAscendingPictures
                    ? PicturesFiles.OrderBy(f => f.LastModified).ToList()
                    : PicturesFiles.OrderByDescending(f => f.LastModified).ToList();
                UpdateFiles(sorted, _picturesTab);
                _isSortByDateAscendingPictures = !_isSortByDateAscendingPictures;
            }
        }

        private void UpdateFiles(List<DesktopFileItemViewModel> sorted, string tabSelected)
        {
            if (tabSelected == _desktopTab)
            {
                DesktopFiles.Clear();
                foreach (var file in sorted)
                {
                    DesktopFiles.Add(file);
                }
            }
            else if (tabSelected == _documentsTab)
            {
                DocumentFiles.Clear();
                foreach (var file in sorted)
                {
                    DocumentFiles.Add(file);
                }
            }
            else if (tabSelected == _picturesTab)
            {
                PicturesFiles.Clear();
                foreach (var file in sorted)
                {
                    PicturesFiles.Add(file);
                }
            }
        }

        private static async Task UpdateUIAsync(Action action)
        {
            try
            {
                if (Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
                {
                    action();
                }
                else
                {
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
                DriveInfo cDrive = new DriveInfo(@"C:\");
                if (cDrive.IsReady)
                {
                    long totalSize = cDrive.TotalSize;
                    long freeSpace = cDrive.AvailableFreeSpace;
                    long usedSpace = totalSize - freeSpace;

                    Log.Information("C: Drive Information - Total Space: {TotalSize} GB, Free Space: {FreeSpace} GB, Used Space: {UsedSpace} GB ",
                        Math.Round((totalSize / (1024.0 * 1024.0 * 1024.0)), 2),
                        Math.Round((freeSpace / (1024.0 * 1024.0 * 1024.0)), 2),
                        Math.Round((usedSpace / (1024.0 * 1024.0 * 1024.0)), 2));
                }
                else
                {
                    Log.Information("C: drive is not ready.");
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