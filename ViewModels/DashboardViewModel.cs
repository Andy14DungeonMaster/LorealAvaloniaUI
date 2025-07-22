// LorealAvaloniaUI.ViewModels/DashboardViewModel.cs
using System;
using System.IO;
using System.Reactive;
using System.Runtime.InteropServices;
using Avalonia.Media;
using LorealAvaloniaUI.Services;
using LorealAvaloniaUI.Views;
using ReactiveUI;
using Serilog;
using System.Reactive.Linq; // Required for WhenAnyValue and Subscribe

namespace LorealAvaloniaUI.ViewModels
{
    public class DashboardViewModel : ReactiveObject
    {
        private readonly NavigationService _navigationService;
        private readonly DownloadInfoService _downloadInfoService;
        private readonly OfficeCacheInfoService _officeCacheInfoService; // Add this field

        private double _usedStorageGB;
        private double _totalStorageGB;
        private DateTime _cleanupDate = FileDeletionTracker.Instance.PreviousLastUsedDate;
        private double _noOfFilesDeleted = FileDeletionTracker.Instance.PreviousDeletedFilesCount;
        private double _clearedSpace = FileDeletionTracker.Instance.PreviousTotalDeletedSizeGB;
        private double _noOfFilesUncached = FileDeletionTracker.Instance.PreviousUncachedFilesCount;
        private double _uncachedSpace = FileDeletionTracker.Instance.PreviousTotalUncachedSizeGB;
        private string _totalAvailableAfterCleanup = $"{FileDeletionTracker.Instance.PreviousAvailableSpace} GB available of {FileDeletionTracker.Instance.PreviousTotalSize} GB";

        private string _downloadsFolderSize;
        private string _officeCacheFolderSize; // New property backing field for Office Cache

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

        public string HeaderMessage => (TotalStorageGB - UsedStorageGB) < 20 ? "URGENT: Critical Low Disk Space Alert!" : "Storage Status";

        public string UserInstructionMessage => (TotalStorageGB - UsedStorageGB) < 20 ? "Your system is running critically low on storage, which may impact performance and stability. We highly recommend you immediately free up space. Please navigate through the Downloads, One Drive, and Office Cache sections to clean up your disk." : "Please navigate through the Downloads, One Drive, and Office Cache sections to clean up your disk.";

        public string StorageUsageText => $"{UsedStorageGB:F0} GB Used of {TotalStorageGB:F0} GB";

        public string FreeStorageText => $"{TotalStorageGB - UsedStorageGB:F0} GB Free";

        public DateTime CleanupDate
        {
            get => _cleanupDate;
            set => this.RaiseAndSetIfChanged(ref _cleanupDate, value);
        }

        public double DeletedFileCount
        {
            get => _noOfFilesDeleted;
            set => this.RaiseAndSetIfChanged(ref _noOfFilesDeleted, value);
        }

        public double ClearedSpace
        {
            get => _clearedSpace;
            set => this.RaiseAndSetIfChanged(ref _clearedSpace, value);
        }

        public double UncachedFileCount
        {
            get => _noOfFilesUncached;
            set => this.RaiseAndSetIfChanged(ref _noOfFilesUncached, value);
        }

        public double UncachedSpace
        {
            get => _uncachedSpace;
            set => this.RaiseAndSetIfChanged(ref _uncachedSpace, value);
        }

        public string TotalAvailableAfterCleanup
        {
            get => _totalAvailableAfterCleanup;
            set => this.RaiseAndSetIfChanged(ref _totalAvailableAfterCleanup, value);
        }

        // Property to display downloads folder size
        public string DownloadsFolderSize
        {
            get => _downloadsFolderSize;
            set => this.RaiseAndSetIfChanged(ref _downloadsFolderSize, value);
        }

        // New property to display Office Cache folder size
        public string OfficeCacheFolderSize
        {
            get => _officeCacheFolderSize;
            set => this.RaiseAndSetIfChanged(ref _officeCacheFolderSize, value);
        }


        // Modify the constructor to accept DownloadInfoService and OfficeCacheInfoService
        public DashboardViewModel(NavigationService navigationService,
                                  DownloadInfoService downloadInfoService,
                                  OfficeCacheInfoService officeCacheInfoService) // Add this parameter
        {
            _navigationService = navigationService;
            _downloadInfoService = downloadInfoService;
            _officeCacheInfoService = officeCacheInfoService; // Assign the injected service

            // Subscribe to changes from the DownloadInfoService
            _downloadInfoService.WhenAnyValue(x => x.TotalDownloadsSize)
                .Subscribe(size => DownloadsFolderSize = size);

            // Subscribe to changes from the OfficeCacheInfoService
            _officeCacheInfoService.WhenAnyValue(x => x.TotalOfficeCacheSize)
                .Subscribe(size => OfficeCacheFolderSize = size);

            FreeUpDownloadsCommand = ReactiveCommand.Create(() =>
            {
                Log.Information("Navigating to DownloadView from dashboard");
                // When navigating, ensure DownloadViewModel can update the service
                _navigationService.Navigate<DownloadViewModel, DownloadView>();
            });
            FreeUpOneDriveCommand = ReactiveCommand.Create(() =>
            {
                Log.Information("Navigating to OneDriveView from dashboard");
                _navigationService.Navigate<OneDriveViewModel, OneDriveView>();
            });
            ShowOutlookDetailsCommand = ReactiveCommand.Create(() =>
            {
                Log.Information("Navigating to OutlookFilesView from dashboard");
                _navigationService.Navigate<OutlookFilesViewModel, OutlookFilesView>();
            });
            ShowOfficeCacheDetailsCommand = ReactiveCommand.Create(() =>
            {
                Log.Information("Navigating to OfficeCacheFilesView from dashboard");
                // When navigating, ensure OfficeFileCacheViewModel can update the service
                _navigationService.Navigate<OfficeFileCacheViewModel, OfficeFileCacheView>();
            });

            LoadDriveInfo();
            // Trigger the calculation of downloads size when the dashboard loads
            _ = _downloadInfoService.CalculateAndSetDownloadsSizeAsync();
            // Trigger the calculation of Office Cache size when the dashboard loads
            _ = _officeCacheInfoService.CalculateAndSetOfficeCacheSizeAsync();
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
                    Log.Warning("Drive '{DriveName}' is not ready. Using fallback values.", driveName);
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