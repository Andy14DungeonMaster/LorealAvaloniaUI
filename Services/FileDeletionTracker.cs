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
        private long _deletedFilesCount;
        private long _totalDeletedSizeBytes;
        private DateTime _lastUsedDate;

        private FileDeletionTracker()
        {
            _statsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LorealAvaloniaUI", "deletion_stats.json");
            Directory.CreateDirectory(Path.GetDirectoryName(_statsFilePath));
            LoadStats();
        }

        public long DeletedFilesCount
        {
            get => _deletedFilesCount;
            private set
            {
                _deletedFilesCount = value;
                SaveStats();
            }
        }

        public double TotalDeletedSizeGB => _totalDeletedSizeBytes / (1024.0 * 1024.0 * 1024.0);

        public long TotalDeletedSizeBytes
        {
            get => _totalDeletedSizeBytes;
            private set
            {
                _totalDeletedSizeBytes = value;
                SaveStats();
            }
        }

        public DateTime LastUsedDate
        {
            get => _lastUsedDate;
            set
            {
                _lastUsedDate = value;
                SaveStats();
                Log.Information("Updated last used date: {LastUsedDate}", _lastUsedDate.ToString("yyyy-MM-dd HH:mm:ss"));
            }
        }

        public void LogDeletion(string fileName, string filePath, long fileSizeBytes)
        {
            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(filePath) || fileSizeBytes < 0)
            {
                Log.Warning("Invalid file deletion log: Name={FileName}, Path={FilePath}, Size={FileSizeBytes}", fileName, filePath, fileSizeBytes);
                return;
            }

            DeletedFilesCount++;
            TotalDeletedSizeBytes += fileSizeBytes;
            Log.Information("File deleted: Name={FileName}, Path={FilePath}, Size={FileSizeGB:F2} GB, Timestamp={Timestamp}, TotalCount={TotalCount}, TotalSize={TotalSizeGB:F2} GB",
                fileName, filePath, fileSizeBytes / (1024.0 * 1024.0 * 1024.0), DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"), DeletedFilesCount, TotalDeletedSizeGB);
        }

        private void LoadStats()
        {
            try
            {
                if (File.Exists(_statsFilePath))
                {
                    var json = File.ReadAllText(_statsFilePath);
                    var stats = JsonSerializer.Deserialize<DeletionStats>(json);
                    _deletedFilesCount = stats?.DeletedFilesCount ?? 0;
                    _totalDeletedSizeBytes = stats?.TotalDeletedSizeBytes ?? 0;
                    _lastUsedDate = stats?.LastUsedDate ?? DateTime.MinValue;
                }
                else
                {
                    _deletedFilesCount = 0;
                    _totalDeletedSizeBytes = 0;
                    _lastUsedDate = DateTime.MinValue;
                    SaveStats();
                }
                Log.Information("Loaded deletion stats. Total files deleted: {TotalCount}, Total size deleted: {TotalSizeGB:F2} GB, Last used: {LastUsedDate}",
                    _deletedFilesCount, TotalDeletedSizeGB, _lastUsedDate == DateTime.MinValue ? "Never" : _lastUsedDate.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load deletion stats");
                _deletedFilesCount = 0;
                _totalDeletedSizeBytes = 0;
                _lastUsedDate = DateTime.MinValue;
            }
        }

        private void SaveStats()
        {
            try
            {
                var stats = new DeletionStats
                {
                    DeletedFilesCount = _deletedFilesCount,
                    TotalDeletedSizeBytes = _totalDeletedSizeBytes,
                    LastUsedDate = _lastUsedDate
                };
                var json = JsonSerializer.Serialize(stats, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_statsFilePath, json);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save deletion stats");
            }
        }

        private class DeletionStats
        {
            public long DeletedFilesCount { get; set; }
            public long TotalDeletedSizeBytes { get; set; }
            public DateTime LastUsedDate { get; set; }
        }
    }
}