using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Input;
using Avalonia.Media;
using ReactiveUI;

namespace LorealAvaloniaUI.ViewModels
{
    public class DashboardViewModel : ReactiveObject
    {
        private double _usedStorageGB;
        private double _totalStorageGB;
        private string _cleanupDate = "26-Jan-2025";
        private string _clearedSpace = "10 GB";
        private string _totalAvailableAfterCleanup = "170 GB of 200 GB";

        public double UsedStorageGB
        {
            get => _usedStorageGB;
            set => this.RaiseAndSetIfChanged(ref _usedStorageGB, value);
        }

        public double TotalStorageGB
        {
            get => _totalStorageGB;
            set => this.RaiseAndSetIfChanged(ref _totalStorageGB, value);
        }

        public double UsagePercentage => TotalStorageGB > 0 ? UsedStorageGB / TotalStorageGB : 0;

        public SolidColorBrush ProgressBarColor => UsagePercentage > 0.9 ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Blue);

        public string HeaderMessage => (TotalStorageGB - UsedStorageGB) < 20 ? "Storage Critically Low" : "Storage Status";

        public string StorageUsageText => $"{UsedStorageGB:F0} GB Used of {TotalStorageGB:F0} GB";

        public string FreeStorageText => $"{TotalStorageGB - UsedStorageGB:F0} GB Free";

        public string CleanupDate
        {
            get => _cleanupDate;
            set => this.RaiseAndSetIfChanged(ref _cleanupDate, value);
        }

        public string ClearedSpace
        {
            get => _clearedSpace;
            set => this.RaiseAndSetIfChanged(ref _clearedSpace, value);
        }

        public string TotalAvailableAfterCleanup
        {
            get => _totalAvailableAfterCleanup;
            set => this.RaiseAndSetIfChanged(ref _totalAvailableAfterCleanup, value);
        }

        public ICommand FreeUpDownloadsCommand { get; }
        public ICommand FreeUpOneDriveCommand { get; }
        public ICommand ShowOutlookDetailsCommand { get; }
        public ICommand ShowOfficeCacheDetailsCommand { get; }

        public DashboardViewModel()
        {
            FreeUpDownloadsCommand = ReactiveCommand.Create(() => { /* Logic to free up downloads space */ });
            FreeUpOneDriveCommand = ReactiveCommand.Create(() => { /* Logic to free up OneDrive space */ });
            ShowOutlookDetailsCommand = ReactiveCommand.Create(() => { /* Logic to show Outlook details */ });
            ShowOfficeCacheDetailsCommand = ReactiveCommand.Create(() => { /* Logic to show Office cache details */ });

            LoadDriveInfo();
        }

        private void LoadDriveInfo()
        {
            try
            {
                string driveName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "C" : "/";
                var drive = new DriveInfo(driveName);
                if (drive.IsReady)
                {
                    // Convert bytes to GB (1 GB = 1024^3 bytes)
                    TotalStorageGB = drive.TotalSize / (1024.0 * 1024.0 * 1024.0);
                    UsedStorageGB = (drive.TotalSize - drive.AvailableFreeSpace) / (1024.0 * 1024.0 * 1024.0);
                }
                else
                {
                    // Fallback values if drive is not ready
                    TotalStorageGB = 200;
                    UsedStorageGB = 186;
                }
            }
            catch (Exception)
            {
                // Handle errors (e.g., drive not found)
                TotalStorageGB = 200;
                UsedStorageGB = 186;
            }
        }
    }
}