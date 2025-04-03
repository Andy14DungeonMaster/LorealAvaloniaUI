
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using LorealAvaloniaUI.Views;
using System.Collections.Generic;
//using static System.Net.WebRequestMethods;
using System.Diagnostics;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace LorealAvaloniaUI.ViewModels
{
    public class OneDriveViewModel : ReactiveObject
    {
        public ObservableCollection<DesktopFileItemViewModel> DesktopFiles { get; } = new ObservableCollection<DesktopFileItemViewModel>();

        public ReactiveCommand<Unit, Unit> FreeSelectedDiskSpaceCommand { get; }

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
                const long OneMB = 1048576; // 1 MB
                var allFiles = Directory.GetFiles(desktopPath, "*.*", SearchOption.AllDirectories);


                foreach (var file in allFiles)
                {
                    var fileInfo = new FileInfo(file);

                    string command = $"attrib \"{fileInfo.FullName}\"";

                    

                    if ( true ) // fileInfo.Length(OneMB)
                    {
                        string result = ExecuteCommand(command);
                        Console.WriteLine(result);

                        if (IsOfflineFile(result))
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

        public static bool IsOfflineFile(string attribOutput)
        {
            // Split the string by spaces, removing empty entries
            string[] parts = attribOutput.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // Check if "U" is present (or absent to determine if it's a system file)
            return ((parts[2]=="P") | (parts[1] == "P")); // True if "P" is *not* found (meaning it is a System File)
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
                    }
                    else
                    {
                        Console.WriteLine($"File not found: {file.FullPath}");
                    }

                    
                }

                catch (Exception ex)
                {
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
                string desktopPath = Path.Combine(
                 Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                 "OneDrive - L'Oréal\\Desktop");

                // Check if the directory exists.
                if (Directory.Exists(desktopPath))
                {
                    //Calculate total number of files.


                    totalDesktopNumber = $"Total no of files > 1 MB: {DesktopFiles.Count.ToString()}";

                    // Calculate the total size of all files.
                    long totalSize = Directory.GetFiles(desktopPath, "*", SearchOption.AllDirectories)
                        .Sum(file => new FileInfo(file).Length);

                    // Format the size (e.g., in MB).
                    TotalDesktopSize = $"Total Size of the files: {totalSize / (1024 * 1024)} MB"; // Or another formatting


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