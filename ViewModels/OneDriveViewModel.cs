
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using LorealAvaloniaUI.Views;
using System.Collections.Generic;
using static System.Net.WebRequestMethods;
using System.Diagnostics;
using System.Threading.Tasks;

namespace LorealAvaloniaUI.ViewModels
{
    public class OneDriveViewModel : ReactiveObject
    {
        public ObservableCollection<DesktopFileItemViewModel> DesktopFiles { get; } = new ObservableCollection<DesktopFileItemViewModel>();

        public ReactiveCommand<Unit, Unit> FreeSelectedDiskSpaceCommand { get; }

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

                    if (fileInfo.Length > OneMB)
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

        }
        public static void ExecuteCommand(string command)
        {
            var processInfo = new ProcessStartInfo("cmd.exe", "/c " + command) // Use "powershell.exe" for PowerShell
            {
                CreateNoWindow = true, // Hide the console window
                UseShellExecute = false, // Necessary for RedirectStandardOutput
                RedirectStandardOutput = true, // Capture output
                RedirectStandardError = true // Capture errors
            };

            try
            {
                using (var process = Process.Start(processInfo))
                {
                    process.WaitForExit(); // Wait for the command to finish

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    Console.WriteLine("Output:\n" + output);

                    if (!string.IsNullOrEmpty(error))
                    {
                        Console.WriteLine("Error:\n" + error);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing command: {ex.Message}");
            }
        }


        public void FreeSelectedDiskSpace()
        {
            var selectedFiles = DesktopFiles.Where(f => f.IsSelected).ToList();

            foreach (var file in selectedFiles)
            {
                try
                {
                    //  bool isOffline = (File.GetAttributes(fileP) & FileAttributes.Offline) == FileAttributes.Offline;

                    ExecuteCommand("attrib +p \"C:\\Users\\alekhya.nandina\\OneDrive - L'Oréal\\Desktop\\Windows App Screens.pptx\"");
                }

                catch (Exception ex)
                {
                    Console.WriteLine(file.ToString());
                }
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