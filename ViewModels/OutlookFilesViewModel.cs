
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Management.Automation;
using System.Text.Json;
using DynamicData;
using ReactiveUI;
using static LorealAvaloniaUI.ViewModels.OneDriveViewModel;

namespace LorealAvaloniaUI.ViewModels
{

    public class OutlookFilesViewModel : ReactiveObject
    {
        public ObservableCollection<OutlookDisplayFiles> OutlookFiles { get; set; } = new();

        
        public OutlookFilesViewModel() 
        {
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
                Console.WriteLine("PowerShell Error:");
                Console.WriteLine(error);
                return;
            }

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var files = JsonSerializer.Deserialize<List<OutlookDisplayFiles>>(output, options);

                //OutlookFiles.AddRange(files);

                Console.WriteLine("OST/PST Files Found:");
                foreach (var file in files)
                {

                    OutlookDisplayFiles outlookFile = new();
                    outlookFile.FileName = file.FileName;
                    outlookFile.FilePath = file.FilePath;
                    outlookFile.FileSizeMB = file.FileSizeMB;
                    outlookFile.Extension = file.Extension;

                    OutlookFiles.Add(outlookFile);

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to parse PowerShell output:");
                Console.WriteLine(ex.Message);
            }

            //OutlookFiles = new ObservableCollection<OutlookDisplayFiles>
            //{
            ////new OutlookDisplayFiles("OutlookFile.ost", "101", "yes", "C:/Users\\alekhya.nandina\\AppData\\Local\\Microsoft\\Outlook"),
            ////new OutlookDisplayFiles("OutlookFile1.pst", "143", "no", "C:\\Users\\alekhya.nandina\\OneDrive - L'Oréal\\Documents\\Outlook Files"),
            ////new OutlookDisplayFiles("OutlookFile2.pst", "300", "no", "C:\\Users\\alekhya.nandina\\OneDrive - L'Oréal\\Documents\\Outlook Files")
            //};

        }
    }

    //public class powershellOutput
    //{

    //    public string FileName { get; set; }

    //    public string FilePath { get; set; }

    //    public string Extension { get; set; }
    //    public string FileSizeMB { get; set; }
    //}




    public class OutlookDisplayFiles: ReactiveObject
    {
        public string FileName { get; set; } = string.Empty;

        public string FilePath { get; set; } = string.Empty;

        public string Extension { get; set; } = string.Empty;
        public string FileSizeMB { get; set; } = string.Empty;



        //public OutlookDisplayFiles(string filename, string size, string extension, string filepath)
        //{
        //    FileName = filename;
        //    FileSizeMB = size;
        //    Extension = extension;
        //    FilePath = filepath;

        //}
    }
}