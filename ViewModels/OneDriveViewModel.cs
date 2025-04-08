
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using LorealAvaloniaUI.Views;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;
using System.Reflection;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;
using Avalonia.Logging;
using Serilog;

namespace LorealAvaloniaUI.ViewModels
{
    public class OneDriveViewModel : ReactiveObject
    {
        public ObservableCollection<DesktopFileItemViewModel> DesktopFiles { get; } = new ObservableCollection<DesktopFileItemViewModel>();

        public ReactiveCommand<Unit, Unit> FreeSelectedDiskSpaceCommand { get; }

        public Dictionary<string, string> fileAttribute = new Dictionary<string, string>();

        // Total Desktop folder size
        private string _totalDesktopSize;
        public string TotalDesktopSize
        {
            get => _totalDesktopSize;
            set => this.RaiseAndSetIfChanged(ref _totalDesktopSize, value);
        }

        private string _totalNumberOfDesktopFiles;

        public string totalDesktopNumber
        {
            get => _totalNumberOfDesktopFiles;
            set => this.RaiseAndSetIfChanged(ref _totalNumberOfDesktopFiles, value);
        }

        public OneDriveViewModel()
        {
            string desktopPath = Path.Combine(
                 Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                 "OneDrive - L'Oréal\\Desktop");

            if (!Directory.Exists(desktopPath))
            {
                Console.WriteLine("desktopPath directory does not exist!");
                return;
            }

            

            try
            {
                Log.Information("One Drive - Desktop Page\n");

                const long OneMB = 1048576; // 1 MB
                var allFiles = Directory.GetFiles(desktopPath, "*.*", SearchOption.AllDirectories);

                string command1 = $"attrib \"C://Users//alekhya.nandina//OneDrive - L'Oréal//Desktop//*.*\" /s";
                string result1 = ExecuteCommand(command1);
                string[] lines = result1.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

                string pattern = @"\s*([^\s])\s*C:\\"; // Correctly escaped backslashes
                string FilePathFromcmd;

                foreach (string line in lines)
                {

                    string charBefore=null;
                    Match match = Regex.Match(line, pattern);

                    if (match.Success)
                    {
                       charBefore = match.Groups[1].Value;
                        Console.WriteLine($"Character before 'C:\\': {charBefore}"); // Output: U
                    }
                    else
                    {
                        Console.WriteLine("'C:\\' not found.");
                    }

                    FilePathFromcmd = line.Substring(line.IndexOf("C:\\"));
                    fileAttribute.Add(FilePathFromcmd, charBefore);



                }

                //foreach (KeyValuePair<string, string> pair in fileAttributes)
                //{
                //    Console.WriteLine($"Key: {pair.Key}, Value: {pair.Value}");
                //}
                Console.WriteLine(result1);


                foreach (var file in allFiles)
                {
                    var fileInfo = new FileInfo(file);

                    if ( true ) // fileInfo.Length(OneMB)
                    {

                        if (fileAttribute[fileInfo.FullName] == "P")
                        {

                        var fileSizeMB = Math.Round((double)fileInfo.Length / OneMB, 2);
                        var rowColor = "#222222".ToString();

                        DesktopFiles.Add(new DesktopFileItemViewModel(
                            fileName: fileInfo.Name,
                            fileSize: fileSizeMB,
                            lastModified: fileInfo.LastWriteTime,
                            rowBackground: rowColor,
                            fullPath: fileInfo.FullName
                        ));

                        }
                    }
                }
            }

            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("Access denied to some files/folders.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            // Initialize commands
            FreeSelectedDiskSpaceCommand = ReactiveCommand.Create(FreeSelectedDiskSpace);

            // calculate Size
            CalculateDesktopSize();

        }

        public static string ExecuteCommand(string command)
        {
            var processInfo = new ProcessStartInfo("cmd.exe", "/c " + command) // Use "powershell.exe" if needed
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            string output = "";
            string error = "";

            try
            {
                using (var process = Process.Start(processInfo))
                {
                    // Asynchronously read the output and error to avoid deadlocks
                    output = process.StandardOutput.ReadToEndAsync().Result; // Use .Result carefully; see explanation below
                    error = process.StandardError.ReadToEndAsync().Result; // Use .Result carefully; see explanation below

                    // Optionally wait for exit to get the exit code (remove if not needed)
                    //process.WaitForExit();
                    //int exitCode = process.ExitCode; //Get the exit code.

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing command: {ex.Message}");
                error += Environment.NewLine + $"Error executing command: {ex.Message}"; // Append error message if needed
            }

            if (!string.IsNullOrEmpty(error))
            {
                output += Environment.NewLine + "Standard Error:" + Environment.NewLine + error;
            }
            return output.Trim();

        }


        public void FreeSelectedDiskSpace()
        {
            var selectedFiles = DesktopFiles.Where(f => f.IsSelected).ToList();
            string changeStatusCommand;

            foreach (var file in selectedFiles)
            {
                try
                {
                    Console.WriteLine($"Trying to UnCache file: {file.FullPath}");
                    if (System.IO.File.Exists(file.FullPath)) // Check if file exists
                    {
                        changeStatusCommand = string.Concat("attrib +u \"", file.FullPath, "\"");
                        ExecuteCommand(changeStatusCommand);  // UnCache file
                        DesktopFiles.Remove(file);         // Remove from UI
                        Console.WriteLine($"{file.FileName} deleted successfully.");
                        Log.Information("Deleteing the file " + file.FileName);
                    }
                    else
                    {
                        Console.WriteLine($"File not found: {file.FullPath}");

                    }

                    
                }

                catch (Exception ex)
                {
                    Log.Error(ex.Message);
                    Console.WriteLine(file.ToString());
                }
            }
            // calculate Size
            CalculateDesktopSize();
            
        }

        private void CalculateDesktopSize()
        {

            try
            {
                // Get the Desktop directory path.  Adapt this to your needs!
                DriveInfo cDrive = new DriveInfo(@"C:\");
                long totalSize=0;

                if (cDrive.IsReady)
                {
                    // Total size of the drive in bytes
                    totalSize = cDrive.TotalSize;

                    // Available free space in bytes
                    long freeSpace = cDrive.AvailableFreeSpace;

                    // Used space in bytes
                    long usedSpace = totalSize - freeSpace;

                    Log.Information("C: Drive Information:");
                    Log.Information("Total Size: " + (totalSize / (1024.0 * 1024.0 * 1024.0)) + "GB");
                    Log.Information("Free Space:" + (freeSpace / (1024.0 * 1024.0 * 1024.0)) + "GB");
                    Log.Information("Used Space:" + (usedSpace / (1024.0 * 1024.0 * 1024.0)) + "GB");
                    Console.WriteLine($"C: Drive Information:");
                    Console.WriteLine($"Total Size: {totalSize / (1024.0 * 1024.0 * 1024.0):F2} GB"); // Convert to GB
                    Console.WriteLine($"Free Space: {freeSpace / (1024.0 * 1024.0 * 1024.0):F2} GB"); // Convert to GB
                    Console.WriteLine($"Used Space: {usedSpace / (1024.0 * 1024.0 * 1024.0):F2} GB"); // Convert to GB

                }
                else
                {
                    Console.WriteLine("C: drive is not ready.");
                }

                string desktopPath = Path.Combine(
                 Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                 "OneDrive - L'Oréal\\Desktop");

                // Check if the directory exists.
                if (Directory.Exists(desktopPath))
                {
                    //Calculate total number of files.
                    totalDesktopNumber = $"Total no of files > 1 MB: {DesktopFiles.Count.ToString()}";


                    // Format the size (e.g., in MB).
                    TotalDesktopSize = $"Total Size of C:\\ Drive: {totalSize / (1024.0 * 1024.0 * 1024.0):F2} GB"; // Or another formatting
                    
                }

                else
                {
                    TotalDesktopSize = "Desktop directory not found.";
                }

            }
            catch (Exception ex)
            {
                TotalDesktopSize = $"Error: {ex.Message}"; // Handle exceptions gracefully
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