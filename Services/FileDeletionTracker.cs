using System;
using System.IO;
using System.Text.Json;
using Serilog;

namespace LorealAvaloniaUI.Services
{
    public class FileDeletionTracker
    {
        private static readonly Lazy<FileDeletionTracker> _instance = new(() => new FileDeletionTracker());
        public static FileDeletionTracker Instance => _instance.Value;

        private readonly string _statsFilePath;

        DriveInfo cDrive = new DriveInfo(@"C:\");

        // Values loaded from JSON (previous sessions)
        public double PreviousDeletedFilesCount { get; private set; }
        public double PreviousTotalDeletedSizeGB { get; private set; }
        public DateTime PreviousLastUsedDate { get; private set; }
        public double PreviousAvailableSpace { get; private set; }
        public double PreviousTotalSize { get; private set; }
        public double PreviousUncachedFilesCount { get; private set; }
        public double PreviousTotalUncachedSizeGB { get; private set; }


        // Values for the current session
        private double _currentSessionDeletedFilesCount;
        private double _currentSessionTotalDeletedSizeBytes;
        private DateTime _currentSessionLastUsedDate;

        private double _currentSessionUncachedFilesCount;
        private double _currentSessionTotalUncachedSizeBytes;

        private FileDeletionTracker()
        {
            _statsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LorealDiskCleanUp", "deletion_stats.json");
            Directory.CreateDirectory(Path.GetDirectoryName(_statsFilePath));

            LoadStats();
        }

        public double CurrentSessionTotalDeletedSizeGB => Math.Round(_currentSessionTotalDeletedSizeBytes / (1024.0 * 1024.0 * 1024.0), 2);


        public bool LogDeletion(string fileName, string filePath, double fileSizeBytes, string section = "")
        {
            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(filePath) || fileSizeBytes < 0)
            {
                Log.Warning("Invalid file deletion log: Name={FileName}, Path={FilePath}, Size={FileSizeBytes}", fileName, filePath, fileSizeBytes);
                return false;
            }

            _currentSessionDeletedFilesCount++;
            _currentSessionTotalDeletedSizeBytes += fileSizeBytes;
            _currentSessionLastUsedDate = DateTime.Now;

            SaveStats();
            return true;
        }

        public bool LogUncaching(string fileName, string filePath, double fileSizeBytes, string section = "")
        {
            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(filePath) || fileSizeBytes < 0)
            {
                Log.Warning("Invalid file deletion log: Name={FileName}, Path={FilePath}, Size={FileSizeBytes}", fileName, filePath, fileSizeBytes);
                return false;
            }

            _currentSessionUncachedFilesCount++;
            _currentSessionTotalUncachedSizeBytes += fileSizeBytes;
            _currentSessionLastUsedDate = DateTime.Now;

            SaveStats();
            return true;
        }

        private void LoadStats()
        {
            try
            {
                if (File.Exists(_statsFilePath))
                {
                    string json = File.ReadAllText(_statsFilePath);
                    DeletionStats stats = JsonSerializer.Deserialize<DeletionStats>(json);

                    if (stats != null)
                    {
                        PreviousDeletedFilesCount = stats.DeletedFilesCount;
                        PreviousTotalDeletedSizeGB = stats.TotalDeletedSizeGB;
                        PreviousUncachedFilesCount = stats.UncachedFilesCount;
                        PreviousTotalUncachedSizeGB = stats.UncachedSizeGB;
                        PreviousLastUsedDate = stats.LastUsedDate;
                        PreviousAvailableSpace = stats.TotalAvailableSize;
                        PreviousTotalSize = stats.TotalSize;
                    }
                    else // Handle deserialization failure
                    {
                        Log.Warning("Failed to deserialize stats.  Using default values.");
                        // ... initialize Previous* values to defaults
                    }
                }
                else
                {
                    // Initialize and save if the file doesn't exist
                    SaveStats();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load deletion stats");
                // Initialize Previous* values to defaults
            }
        }

        private void SaveStats()
        {
            try
            {

                DeletionStats stats = new DeletionStats
                {
                    DeletedFilesCount = _currentSessionDeletedFilesCount,
                    TotalDeletedSizeGB  = Math.Round((_currentSessionTotalDeletedSizeBytes) / (1024.0),3),
                    LastUsedDate = _currentSessionLastUsedDate,
                    UncachedFilesCount = _currentSessionUncachedFilesCount,
                    UncachedSizeGB = Math.Round((_currentSessionTotalUncachedSizeBytes) / (1024.0), 3),
                    TotalAvailableSize = Math.Round((cDrive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0)), 2),
                    TotalSize = Math.Round((cDrive.TotalSize / (1024.0 * 1024.0 * 1024.0)), 2),

                };

                string json = JsonSerializer.Serialize(stats, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_statsFilePath, json);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save deletion stats");
            }
        }


        private class DeletionStats
        {
            public double DeletedFilesCount { get; set; }
            public double TotalDeletedSizeGB { get; set; }
            public DateTime LastUsedDate { get; set; }
            public double TotalAvailableSize { get; set; }
            public double TotalSize { get; set; }

            public double UncachedFilesCount { get; set; }
            public double UncachedSizeGB { get; set; }
        }
    }
}