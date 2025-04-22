
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text.Json;
using DynamicData;
using ReactiveUI;
using static LorealAvaloniaUI.ViewModels.OneDriveViewModel;
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


                string psScript = @"
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
@($results) | ConvertTo-Json -Compress
";
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{psScript}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                var process = new Process { StartInfo = psi };
                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrWhiteSpace(error))
                {
                    Log.Information("PowerShell Error:");
                    Log.Error(error);
                    return;
                }




                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var files = JsonSerializer.Deserialize<List<OutlookDisplayFiles>>(output, options);

                //OutlookFiles.AddRange(files);

                Log.Information("OST/PST Files Found:");
                foreach (var file in files)
                {

                    OutlookDisplayFiles outlookFile = new();
                    outlookFile.FileName = file.FileName;
                    outlookFile.FilePath = file.FilePath;
                    outlookFile.FileSizeMB = file.FileSizeMB;
                    outlookFile.Extension = file.Extension;

                    OutlookFiles.Add(outlookFile);
                    Log.Information("Outlook file found: {FilePath}", outlookFile.FilePath);

                }
            }
            catch (Exception ex)
            {
                Log.Error("Failed to parse PowerShell output:");
                Log.Error(ex.Message);
            }

        }
    }



    public class OutlookDisplayFiles: ReactiveObject
    {
        public string FileName { get; set; } = string.Empty;

        public string FilePath { get; set; } = string.Empty;

        public string Extension { get; set; } = string.Empty;
        public string FileSizeMB { get; set; } = string.Empty;

    }
}