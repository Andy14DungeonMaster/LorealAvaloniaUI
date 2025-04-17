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
        // Desktop Tab
        private readonly string desktopPath;
        private readonly ConcurrentDictionary<string, string> _fileAttributes = new();
        private string _totalDesktopSize;
        private string _totalNumberOfDesktopFiles;
        private bool _selectAll;
        private bool _isSortByNameAscending = true;
        private bool _isSortBySizeAscending = true;
        private bool _isSortByDateAscending = true;

        // Documents Tab
        private readonly string documentsPath;
        private string _totaldocumentsSize;
        private string _totalNumberOfDocumentsFiles;
        private bool _selectAllDocuments;

        // Pictures Tab
        private readonly string picturesPath;
        private string _totalpicturesSize;
        private string _totalNumberOfpicturesFiles;
        private bool _selectAllPictures;



        //private bool _isSortByNameAscendingDocuments = true;
        //private bool _isSortBySizeAscendingDocuments = true;
        //private bool _isSortByDateAscendingDocuments = true;

        private string _desktopTab = "Desktop";
        private string _documentsTab = "Documents";
        private string _picturesTab = "Pictures";


        public ObservableCollection<DesktopFileItemViewModel> DesktopFiles { get; } = new();

        public ObservableCollection<DesktopFileItemViewModel> DocumentFiles { get; } = new();

        public ObservableCollection<DesktopFileItemViewModel> PicturesFiles { get; } = new();

        public ReactiveCommand<Unit, Unit> FreeSelectedDiskSpaceCommand { get; }
        public ReactiveCommand<Unit, Unit> SortByNameCommand { get; }
        public ReactiveCommand<Unit, Unit> SortBySizeCommand { get; }
        public ReactiveCommand<Unit, Unit> SortByDateCommand { get; }

        public ReactiveCommand<Unit, Unit> FreeSelectedDiskSpaceCommandDocuments { get; }
        public ReactiveCommand<Unit, Unit> SortByNameDocumentsCommand { get; }
        public ReactiveCommand<Unit, Unit> SortBySizeDocumentsCommand { get; }
        public ReactiveCommand<Unit, Unit> SortByDateDocumentsCommand { get; }

        public ReactiveCommand<Unit, Unit> FreeSelectedDiskSpaceCommandPictures { get; }
        public ReactiveCommand<Unit, Unit> SortByNamePicturesCommand { get; }
        public ReactiveCommand<Unit, Unit> SortBySizePicturesCommand { get; }
        public ReactiveCommand<Unit, Unit> SortByDatePicturesCommand { get; }



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
            get => _totaldocumentsSize;
            set => this.RaiseAndSetIfChanged(ref _totaldocumentsSize, value);
        }

        public string TotalDocumentNumber
        {
            get => _totalNumberOfDocumentsFiles;
            set => this.RaiseAndSetIfChanged(ref _totalNumberOfDocumentsFiles, value);
        }

        public string TotalPicturesSize
        {
            get => _totalpicturesSize;
            set => this.RaiseAndSetIfChanged(ref _totalpicturesSize, value);
        }

        public string TotalPicturesNumber
        {
            get => _totalNumberOfpicturesFiles;
            set => this.RaiseAndSetIfChanged(ref _totalNumberOfpicturesFiles, value);
        }

        public bool SelectAll
        {
            get => _selectAll;
            set => this.RaiseAndSetIfChanged(ref _selectAll, value);
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

        public OneDriveViewModel()
        {
            desktopPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "OneDrive - L'Oréal\\Desktop");

            documentsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "OneDrive - L'Oréal\\Documents");

            picturesPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "OneDrive - L'Oréal\\Pictures");

            _totalDesktopSize = string.Empty;
            _totalNumberOfDesktopFiles = string.Empty;

            _totaldocumentsSize = string.Empty;
            _totalNumberOfDocumentsFiles = string.Empty;

            _totalpicturesSize = string.Empty;
            _totalNumberOfpicturesFiles = string.Empty;



            FreeSelectedDiskSpaceCommand = ReactiveCommand.CreateFromTask(FreeSelectedDiskSpaceAsync);
            SortByNameCommand = ReactiveCommand.Create(() => SortByName(_desktopTab));
            SortBySizeCommand = ReactiveCommand.Create(() => SortBySize(_desktopTab));
            SortByDateCommand = ReactiveCommand.Create(() => SortByDate(_desktopTab));

            FreeSelectedDiskSpaceCommandDocuments = ReactiveCommand.CreateFromTask(FreeSelectedDiskSpaceDocumentsAsync);
            SortByNameDocumentsCommand = ReactiveCommand.Create(() => SortByName(_documentsTab));
            SortBySizeDocumentsCommand = ReactiveCommand.Create(() => SortBySize(_documentsTab));
            SortByDateDocumentsCommand = ReactiveCommand.Create(() => SortByDate(_documentsTab));

            FreeSelectedDiskSpaceCommandPictures = ReactiveCommand.CreateFromTask(FreeSelectedDiskSpacePicturesAsync);
            SortByNamePicturesCommand = ReactiveCommand.Create(() => SortByName(_picturesTab));
            SortBySizePicturesCommand = ReactiveCommand.Create(() => SortBySize(_picturesTab));
            SortByDatePicturesCommand = ReactiveCommand.Create(() => SortByDate(_picturesTab));

            this.WhenAnyValue(x => x.SelectAll)
                .Subscribe(selectAll =>
                {
                    foreach (var file in DesktopFiles)
                    {
                        file.IsSelected = selectAll;
                    }
                });

            this.WhenAnyValue(x => x.SelectAllDocuments)
                .Subscribe(selectAllDocuments =>
                {
                    foreach (var file in DocumentFiles)
                    {
                        file.IsSelected = selectAllDocuments;
                    }
                });

            this.WhenAnyValue(x => x.SelectAllPictures)
                .Subscribe(selectAllPictures =>
                {
                    foreach (var file in PicturesFiles)
                    {
                        file.IsSelected = selectAllPictures;
                    }
                });

            // Perform async initialization without blocking
            InitializeAsync().GetAwaiter().OnCompleted(() => { });
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


        private async Task InitializeAsync()
        {
            if (!Directory.Exists(desktopPath))
            {
                Log.Error("Desktop directory does not exist: {DesktopPath}", desktopPath);
                await UpdateUIAsync(() => TotalDesktopSize = "Desktop directory not found.");
                return;
            }

            if (!Directory.Exists(documentsPath))
            {
                Log.Error("Documents directory does not exist: {DocumentsPath}", documentsPath);
                await UpdateUIAsync(() => TotalDesktopSize = "Documents directory not found.");
                return;
            }

            if (!Directory.Exists(picturesPath))
            {
                Log.Error("Pictures directory does not exist: {PicturesPath}", picturesPath);
                await UpdateUIAsync(() => TotalDesktopSize = "Pictures directory not found.");
                return;
            }

            try
            {

                await Task.WhenAll(
                    LoadFileAttributesAsync(desktopPath)
                );

                Log.Information("Found {Count} cached desktop file(s) larger than 100 MB", DesktopFiles.Count);
           

                await Task.WhenAll(
                    LoadFileAttributesAsync(documentsPath)
                );
                Log.Information("Found {Count} cached documents file(s) larger than 100 MB", DocumentFiles.Count);

                await Task.WhenAll(
                   LoadFileAttributesAsync(picturesPath)
               );

                Log.Information("Found {Count} cached pictures file(s) larger than 100 MB", PicturesFiles.Count);

                await Task.WhenAll(
                    CalculateSizeAsync() //Separated call to calculate Size on tab open
                    );

            }
            catch (Exception ex)
            {
                Log.Error(ex, "Initialization failed");
                await UpdateUIAsync(() => TotalDesktopSize = $"Error: {ex.Message}");
            }
        }

        private async Task LoadFileAttributesAsync(string path)
        {
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

            try
            {
                await Task.Run(async () =>
                {
                    foreach (var file in files)
                    {
                        if (_fileAttributes.TryGetValue(file, out var attr) && attr == "P")
                        {
                            var fileInfo = new FileInfo(file);
                            if (fileInfo.Length >= (OneMB * 100)) // Only add files Size > 100 MB
                            {
                                var fileSizeMB = Math.Round((double)fileInfo.Length / OneMB, 2);
                                await UpdateUIAsync(() =>
                                {
                                    if (path == desktopPath)
                                    {
                                        DesktopFiles.Add(new DesktopFileItemViewModel(
                                            fileInfo.Name,
                                            fileSizeMB,
                                            fileInfo.LastWriteTime,
                                            "#222222",
                                            fileInfo.FullName));
                                    }
                                    else if (path == documentsPath)
                                    {
                                        DocumentFiles.Add(new DesktopFileItemViewModel(
                                                fileInfo.Name,
                                                fileSizeMB,
                                                fileInfo.LastWriteTime,
                                                "#222222",
                                                fileInfo.FullName));
                                    }

                                    else if (path == picturesPath)
                                    {
                                        PicturesFiles.Add(new DesktopFileItemViewModel(
                                                fileInfo.Name,
                                                fileSizeMB,
                                                fileInfo.LastWriteTime,
                                                "#222222",
                                                fileInfo.FullName));
                                    }
                                    else
                                    {
                                        Log.Information("Not able to load files");
                                    }

                                });
                            }
                        }
                    }
                });
               

            }

            catch (Exception ex)
            {
                Log.Error($"Error loading files: {ex.Message}");
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

        private async Task FreeDiskSpaceAsync(ObservableCollection<DesktopFileItemViewModel> Files, string _tabSelected)
        {

            Log.Information("** Uncache action initiated **");
            Log.Information("SIZE OF THE DISK BEFORE DELETE");
            LogSystemInformation(); // Log Size of disk before delete task

            var selectedFiles = Files.Where(f => f.IsSelected).ToList();


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
                            if ( _tabSelected == _desktopTab )
                            {
                                DesktopFiles.Remove(file);
                            }
                            else if (_tabSelected == _documentsTab)
                            {
                                DocumentFiles.Remove(file);
                            }
                            else if (_tabSelected == _picturesTab)
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
            LogSystemInformation(); // Log Size of disk before after task
            await CalculateSizeAsync();
        }

       
        private async Task CalculateSizeAsync()
        {
            try
            {
                var cDrive = new DriveInfo("C");
                if (!cDrive.IsReady)
                {
                    Log.Warning("C: drive is not ready");
                    await UpdateUIAsync(() => TotalDesktopSize = "C: drive is not ready");
                    await UpdateUIAsync(() => TotalDocumentSize = "C: drive is not ready");
                    await UpdateUIAsync(() => TotalPicturesSize = "C: drive is not ready");
                    return;
                }

                long totalSize = cDrive.TotalSize;
                long freeSpace = cDrive.AvailableFreeSpace;
                double totalSizeGB = totalSize / (1024.0 * 1024.0 * 1024.0);


                    await UpdateUIAsync(() =>
                    {
                        TotalDesktopNumber = $"Total files > 100 MB: {DesktopFiles.Count}";
                        TotalDesktopSize = $"C: Drive Size: {totalSizeGB:F2} GB";
                    });

                    await UpdateUIAsync(() =>
                    {
                        TotalDocumentNumber = $"Total files > 100 MB: {DocumentFiles.Count}";
                        TotalDocumentSize = $"C: Drive Size: {totalSizeGB:F2} GB";
                    });

                await UpdateUIAsync(() =>
                {
                    TotalPicturesNumber = $"Total files > 100 MB: {PicturesFiles.Count}";
                    TotalPicturesSize = $"C: Drive Size: {totalSizeGB:F2} GB";
                });

               


            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to calculate desktop size");
                await UpdateUIAsync(() => TotalDesktopSize = $"Error: {ex.Message}");
                await UpdateUIAsync(() => TotalDocumentSize = $"Error: {ex.Message}");
            }
        }

        private void SortByName(string _tabSelected)
        {
            if (_tabSelected == _desktopTab)
            {
                var sorted = _isSortByNameAscending
               ? DesktopFiles.OrderBy(f => f.FileName).ToList()
               : DesktopFiles.OrderByDescending(f => f.FileName).ToList();
                UpdateDesktopFiles(sorted, _desktopTab);
                _isSortByNameAscending = !_isSortByNameAscending;
            }
            else if (_tabSelected ==_documentsTab)
            {
                var sorted = _isSortByNameAscending
              ? DocumentFiles.OrderBy(f => f.FileName).ToList()
              : DocumentFiles.OrderByDescending(f => f.FileName).ToList();
                UpdateDesktopFiles(sorted, _documentsTab);
                _isSortByNameAscending = !_isSortByNameAscending;
            }
            else if (_tabSelected == _picturesTab)
            {
                var sorted = _isSortByNameAscending
              ? PicturesFiles.OrderBy(f => f.FileName).ToList()
              : PicturesFiles.OrderByDescending(f => f.FileName).ToList();
                UpdateDesktopFiles(sorted, _documentsTab);
                _isSortByNameAscending = !_isSortByNameAscending;
            }

        }

        private void SortBySize(string _tabSelected)
        {
            if (_tabSelected == _desktopTab)
            {
                var sorted = _isSortBySizeAscending
                ? DesktopFiles.OrderBy(f => f.FileSize).ToList()
                : DesktopFiles.OrderByDescending(f => f.FileSize).ToList();
                UpdateDesktopFiles(sorted, _desktopTab);
                _isSortBySizeAscending = !_isSortBySizeAscending;
            }
            else if (_tabSelected == _documentsTab)
            {
                var sorted = _isSortBySizeAscending
                ? DocumentFiles.OrderBy(f => f.FileSize).ToList()
                : DocumentFiles.OrderByDescending(f => f.FileSize).ToList();
                UpdateDesktopFiles(sorted, _documentsTab);
                _isSortBySizeAscending = !_isSortBySizeAscending;
            }
            else if (_tabSelected == _picturesTab)
            {
                var sorted = _isSortBySizeAscending
                ? PicturesFiles.OrderBy(f => f.FileSize).ToList()
                : PicturesFiles.OrderByDescending(f => f.FileSize).ToList();
                UpdateDesktopFiles(sorted, _documentsTab);
                _isSortBySizeAscending = !_isSortBySizeAscending;
            }

        }

        private void SortByDate(string _tabSelected)
        {
            if (_tabSelected == _desktopTab)
            {
                var sorted = _isSortByDateAscending
                ? DesktopFiles.OrderBy(f => f.LastModified).ToList()
                : DesktopFiles.OrderByDescending(f => f.LastModified).ToList();
                UpdateDesktopFiles(sorted, _desktopTab);
                _isSortByDateAscending = !_isSortByDateAscending;
            }
            else if (_tabSelected == _documentsTab)
            {
                var sorted = _isSortBySizeAscending
               ? DocumentFiles.OrderBy(f => f.FileSize).ToList()
               : DocumentFiles.OrderByDescending(f => f.FileSize).ToList();
                UpdateDesktopFiles(sorted, _documentsTab);
                _isSortBySizeAscending = !_isSortBySizeAscending;
            }
            else if (_tabSelected == _picturesTab)
            {
                var sorted = _isSortBySizeAscending
               ? PicturesFiles.OrderBy(f => f.FileSize).ToList()
               : PicturesFiles.OrderByDescending(f => f.FileSize).ToList();
                UpdateDesktopFiles(sorted, _documentsTab);
                _isSortBySizeAscending = !_isSortBySizeAscending;
            }
        }

        private void UpdateDesktopFiles(List<DesktopFileItemViewModel> sorted, string _tabSelected)
        {
            if (_tabSelected == _desktopTab)
            {
                DesktopFiles.Clear();
                foreach (var file in sorted)
                {
                    DesktopFiles.Add(file);
                }
            }
            else if (_tabSelected == _documentsTab)
            {
                DocumentFiles.Clear();
                foreach (var file in sorted)
                {
                    DocumentFiles.Add(file);
                }
            }
            else if (_tabSelected == _picturesTab)
            {
                PicturesFiles.Clear();
                foreach (var file in sorted)
                {
                    DocumentFiles.Add(file);
                }
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



                    Log.Information("C: Drive Information - Total Space: {TotalSize} GB, Free Space: {FreeSpace} GB, Used Space: {UsedSpace} GB ", Math.Round((totalSize / (1024.0 * 1024.0 * 1024.0)), 2),
                        Math.Round((freeSpace / (1024.0 * 1024.0 * 1024.0)), 2),
                        Math.Round((usedSpace / (1024.0 * 1024.0 * 1024.0)), 2));

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