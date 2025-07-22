using ReactiveUI;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Serilog; // Assuming Serilog is configured for logging

namespace LorealAvaloniaUI.Services
{
    public class DownloadInfoService : ReactiveObject
    {
        private string _totalDownloadsSize = "Calculating..."; // Initial state
        public string TotalDownloadsSize
        {
            get => _totalDownloadsSize;
            private set => this.RaiseAndSetIfChanged(ref _totalDownloadsSize, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => this.RaiseAndSetIfChanged(ref _isLoading, value);
        }

        /// <summary>
        /// Calculates the total size of the user's Downloads folder and updates the TotalDownloadsSize property.
        /// </summary>
        public async Task CalculateAndSetDownloadsSizeAsync()
        {
            IsLoading = true; // Optional: Add an IsLoading property if you want to show a loading state
            try
            {
                string downloadsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads"
                );

                if (!Directory.Exists(downloadsPath))
                {
                    TotalDownloadsSize = "Downloads folder not found.";
                    Log.Error("Downloads directory does not exist: {DownloadsPath}", downloadsPath);
                    return;
                }

                long totalSizeInBytes = await Task.Run(() =>
                {
                    return Directory.GetFiles(downloadsPath, "*", SearchOption.AllDirectories)
                        .Sum(file =>
                        {
                            try
                            {
                                // Get file info and length, handle potential access errors
                                return new FileInfo(file).Length;
                            }
                            catch (UnauthorizedAccessException uae)
                            {
                                Log.Warning("Access denied to file '{File}': {Ex}", file, uae.Message);
                                return 0; // Skip files we can't access
                            }
                            catch (FileNotFoundException fnf)
                            {
                                Log.Warning("File not found '{File}': {Ex}", file, fnf.Message);
                                return 0; // Skip files that might have been deleted mid-scan
                            }
                            catch (Exception ex)
                            {
                                Log.Error("Error getting size for file '{File}': {Ex}", file, ex.Message);
                                return 0; // Catch any other exceptions
                            }
                        });
                });

                // Convert bytes to GB for dashboard display
                double totalSizeGB = totalSizeInBytes / (1024.0 * 1024.0 * 1024.0);
                TotalDownloadsSize = $"{totalSizeGB:0.00}";
                Log.Information($"Downloads folder size calculated: {TotalDownloadsSize}");
            }
            catch (Exception ex)
            {
                TotalDownloadsSize = $"Error: {ex.Message}";
                Log.Error("Error calculating downloads size in service: {Ex}", ex);
            }
            finally
            {
                // IsLoading = false; // Optional: Set loading state to false
            }
        }
    }
}