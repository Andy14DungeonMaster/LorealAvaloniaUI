using System;
using System.IO;
using System.Reactive;
using System.Runtime.InteropServices;
using Avalonia.Media;
using LorealAvaloniaUI.Services;
using LorealAvaloniaUI.Views;
using ReactiveUI;
using Serilog;

namespace LorealAvaloniaUI.ViewModels
{
    public class DashboardViewModel : ReactiveObject
    {
        private readonly NavigationService _navigationService;
        private double _usedStorageGB;
        private double _totalStorageGB;
        private string _cleanupDate = "26-Jan-2025";
        private string _clearedSpace = "10 GB";
        private string _totalAvailableAfterCleanup = "170 GB of 200 GB";
        public ReactiveCommand<Unit, Unit> FreeUpDownloadsCommand { get; }
        public ReactiveCommand<Unit, Unit> FreeUpOneDriveCommand { get; }
        public ReactiveCommand<Unit, Unit> ShowOutlookDetailsCommand { get; }
        public ReactiveCommand<Unit, Unit> ShowOfficeCacheDetailsCommand { get; }

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

        public SolidColorBrush StorageTextColor => UsagePercentage > 0.9 ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.White);

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

        public long DeletedFilesCount => FileDeletionTracker.Instance.DeletedFilesCount;

        public double TotalDeletedSizeGB => FileDeletionTracker.Instance.TotalDeletedSizeGB;

        public string LastUsedDate => FileDeletionTracker.Instance.LastUsedDate == DateTime.MinValue ? "Never" : FileDeletionTracker.Instance.LastUsedDate.ToString("yyyy-MM-dd HH:mm:ss");

        public DashboardViewModel(NavigationService navigationService)
        {
            _navigationService = navigationService;
            FreeUpDownloadsCommand = ReactiveCommand.Create(() =>
            {
                Log.Information("Navigating to DownloadView");
                _navigationService.Navigate<DownloadViewModel, DownloadView>();
            });
            FreeUpOneDriveCommand = ReactiveCommand.Create(() =>
            {
                Log.Information("Navigating to OneDriveView");
                _navigationService.Navigate<OneDriveViewModel, OneDriveView>();
            });
            ShowOutlookDetailsCommand = ReactiveCommand.Create(() =>
            {
                Log.Information("Navigating to OutlookFilesView");
                _navigationService.Navigate<OutlookFilesViewModel, OutlookFilesView>();
            });
            ShowOfficeCacheDetailsCommand = ReactiveCommand.Create(() =>
            {
                Log.Information("Navigating to OfficeFileCacheView");
                _navigationService.Navigate<OfficeFileCacheViewModel, OfficeFileCacheView>();
            });

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
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load drive info");
                TotalStorageGB = 200;
                UsedStorageGB = 186;
            }
        }
    }
}