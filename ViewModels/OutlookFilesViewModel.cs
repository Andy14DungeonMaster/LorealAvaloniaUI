using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ReactiveUI;
using Serilog;

namespace LorealAvaloniaUI.ViewModels
{
    public class OutlookFilesViewModel : ReactiveObject
    {
        public ObservableCollection<OutlookDisplayFiles> OutlookFiles { get; set; } = new();

        private string _outlookStatus = "";
        public string outlookStatus
        {
            get => _outlookStatus;
            set => this.RaiseAndSetIfChanged(ref _outlookStatus, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => this.RaiseAndSetIfChanged(ref _isLoading, value);
        }
        public OutlookFilesViewModel()
        {
            try
            {
                string outlookPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "AppData\\Local\\Microsoft\\Outlook"
                );

                if (!Directory.Exists(outlookPath))
                {
                    Log.Error("Outlook directory does not exist: {OutlookPath}", outlookPath);
                    outlookStatus = $"Outlook directory does not exist: {outlookPath}";
                    return;
                }

                if (!Directory.EnumerateFiles(outlookPath, "*.ost").Any())
                {
                    Log.Error("Outlook not configured: {OutlookPath} ", outlookPath);
                    outlookStatus = $"Outlook not configured: {outlookPath}";
                    return;
                }
            }

            catch(Exception ex)
            {
                Log.Error("{exception}",ex);
            }


          _ =  LoadOutlookFilesAsync(); // fire and forget
        }

        private async Task LoadOutlookFilesAsync()
        {
            IsLoading = true; // Show loading indicator

            string psScript = @"
$outlookProcesses = Get-Process -Name Outlook -ErrorAction SilentlyContinue

if (-not $outlookProcesses) {
    # Outlook is not running, return empty array and exit
    Write-Output '[]'
    exit
}
Add-Type -AssemblyName 'Microsoft.Office.Interop.Outlook'
$outlook = New-Object -ComObject Outlook.Application
$namespace = $outlook.GetNamespace('MAPI')
$stores = $namespace.Stores
$results = @()
foreach ($store in $stores) {
    $filePath = $store.FilePath
    if ($filePath -like '*.pst' -or $filePath -like '*.ost') {
        $fileInfo = Get-Item $filePath
        $results += [PSCustomObject]@{
            FileName   = $fileInfo.Name
            FilePath   = $fileInfo.FullName
            Extension  = $fileInfo.Extension
            FileSizeMB = '{0:N2}' -f ($fileInfo.Length / 1MB)
        }
    }
}

if ($results.Count -eq 1) {  # Check if only one item
    ConvertTo-Json -InputObject @($results) -Compress # Wrap in array
} else {
    ConvertTo-Json -InputObject $results -Compress # Already an array
}

";

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{psScript}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null) return;

                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (string.IsNullOrWhiteSpace(output))
                {
                    Log.Information("Output of powershell is empty string");

                    return;
                }

                if (!string.IsNullOrWhiteSpace(error))
                {
                    Log.Information("PowerShell Error:");
                    Log.Error(error);
                    return;
                }

                if (output.Trim() == "[]")
                {
                    Log.Information("Outlook in not running. OST/PST files not accessed");
                    outlookStatus = $"Outlook is not running. Open outlook on your device. Navigate or click again on \"Outlook Files\" in the app to access OST/PST files.";
                    return;
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var files = JsonSerializer.Deserialize<OutlookDisplayFiles[]>(output, options);

                if (files != null)
                {
                    foreach (var file in files)
                    {
                        OutlookFiles.Add(file);
                        Log.Information("Found file {FileName} of Size {Size} at {Location}", file.FileName,file.FileSizeMB, file.FilePath);
                    }

                }
            }

            catch (JsonException ex)
            {
                Log.Error("Json Error: {Ex}", ex);
            }
            catch (Exception ex)
            {
                Log.Error("Failed to run PowerShell script:");
                Log.Error(ex.Message);
            }
            finally
            {
                IsLoading = false;
            }

        }
    }

    public class OutlookDisplayFiles : ReactiveObject
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public string FileSizeMB { get; set; } = string.Empty;
    }
}
