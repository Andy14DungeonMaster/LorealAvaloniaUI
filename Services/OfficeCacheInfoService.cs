// LorealAvaloniaUI.Services/OfficeCacheInfoService.cs
using ReactiveUI;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Serilog; // Assuming Serilog is configured for logging

namespace LorealAvaloniaUI.Services
{
    public class OfficeCacheInfoService : ReactiveObject
    {
        private string _totalOfficeCacheSize = "Calculating Office Cache..."; // Initial state
        public string TotalOfficeCacheSize
        {
            get => _totalOfficeCacheSize;
            private set => this.RaiseAndSetIfChanged(ref _totalOfficeCacheSize, value);
        }

        /// <summary>
        /// Calculates the total size of the Office cache folder and updates the TotalOfficeCacheSize property.
        /// </summary>
        public async Task CalculateAndSetOfficeCacheSizeAsync()
        {
            try
            {
                // IMPORTANT: This path needs to be confirmed and adjusted based on
                // how your application identifies and accesses the Office cache.
                // The path you provided in OfficeFileCacheViewModel is:
                string officeCachePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "AppData\\Local\\Microsoft\\Office\\16.0\\OfficeFileCache\\0\\0"
                );

                if (!Directory.Exists(officeCachePath))
                {
                    TotalOfficeCacheSize = $"Office cache directory does not exist or is not at expected path: {officeCachePath}";
                    Log.Information("Office cache directory does not exist or is not at expected path: {OfficeCachePath}", officeCachePath);
                    return;
                }

                long totalSizeInBytes = await Task.Run(() =>
                {
                    // Sum the lengths of all files within the specified directory and its subdirectories
                    DirectoryInfo dirInfo = new DirectoryInfo(officeCachePath);
                    if (!dirInfo.Exists) return 0; // Handle case where directory might disappear
                    return dirInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                        .Sum(file =>
                        {
                            try
                            {
                                return file.Length;
                            }
                            catch (UnauthorizedAccessException uae)
                            {
                                Log.Warning("Access denied to Office cache file '{File}': {Ex}", file.FullName, uae.Message);
                                return 0;
                            }
                            catch (FileNotFoundException fnf)
                            {
                                Log.Warning("Office cache file not found '{File}': {Ex}", file.FullName, fnf.Message);
                                return 0;
                            }
                            catch (Exception ex)
                            {
                                Log.Error("Error getting size for Office cache file '{File}': {Ex}", file.FullName, ex.Message);
                                return 0;
                            }
                        });
                });

                // Convert bytes to GB for dashboard display (or MB if preferred)
                // Using MB as per your original ViewModel's display format for total size
                double totalSizeGB = totalSizeInBytes / (1024.0 * 1024.0 * 1024.0);
                TotalOfficeCacheSize = $"{totalSizeGB:0.00}";
                Log.Information($"Office cache folder size calculated: {TotalOfficeCacheSize}");
            }
            catch (Exception ex)
            {
                TotalOfficeCacheSize = $"Error: {ex.Message}";
                Log.Error("Error calculating Office cache size in service: {Ex}", ex);
            }
        }
    }
}