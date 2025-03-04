using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using Microsoft.Extensions.Configuration;
using ReactiveUI;

namespace LorealAvaloniaUI.ViewModels
{
    public class DownloadViewModel : ReactiveObject
    {
        private readonly IConfiguration _configuration;

        public ObservableCollection<FileItemViewModel> Files { get; } = new();

        public string SelectedSize => $"{Files.Where(f => f.IsSelected).Sum(f => f.FileSize):0.##} MB";

        public DownloadViewModel(IConfiguration configuration)
        {
            _configuration = configuration;
            LoadFiles();
        }

        private void LoadFiles()
        {
            // Read values from appsettings.json
            string scriptPath = _configuration["PowerShell:ScriptPath"];
            string directoryPath = _configuration["PowerShell:DirectoryPath"];

            if (string.IsNullOrEmpty(scriptPath) || string.IsNullOrEmpty(directoryPath))
            {
                Debug.WriteLine("PowerShell script path or directory path is missing in appsettings.json.");
                return;
            }

            var files = LoadFilesFromPowerShell(scriptPath, directoryPath);
            foreach (var file in files)
            {
                Files.Add(file);
            }

            this.RaisePropertyChanged(nameof(SelectedSize));
        }

        private ObservableCollection<FileItemViewModel> LoadFilesFromPowerShell(string scriptPath, string directoryPath)
        {
            var files = new ObservableCollection<FileItemViewModel>();

            try
            {
                using var ps = PowerShell.Create();
                ps.AddCommand(scriptPath)
                  .AddArgument(directoryPath);

                var results = ps.Invoke();
                foreach (var result in results)
                {
                    var fileData = result.BaseObject.ToString()?.Split('|');
                    if (fileData?.Length == 3)
                    {
                        files.Add(new FileItemViewModel(
                            fileData[0],
                            double.Parse(fileData[1]),
                            DateTime.Parse(fileData[2])
                        ));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error running PowerShell script: {ex.Message}");
            }

            return files;
        }
    }

    public class FileItemViewModel : ReactiveObject
    {
        public string FileName { get; }
        public double FileSize { get; }
        public DateTime LastModified { get; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => this.RaiseAndSetIfChanged(ref _isSelected, value);
        }

        public string RowBackground => IsSelected ? "#444" : "#222"; // Highlight selected row

        public FileItemViewModel(string fileName, double fileSize, DateTime lastModified)
        {
            FileName = fileName;
            FileSize = fileSize;
            LastModified = lastModified;
        }
    }
}