using System;
using System.Collections.ObjectModel;
using ReactiveUI;

namespace LorealAvaloniaUI.ViewModels
{
    public class DownloadViewModel : ReactiveObject
    {
        public ObservableCollection<FileItemViewModel> Files { get; }
        
        public DownloadViewModel()
        {
            Files = new ObservableCollection<FileItemViewModel>
            {
                new FileItemViewModel("Report.pdf", 750, DateTime.Now.AddDays(-2), "#222222"),
                new FileItemViewModel("Video.mp4", 1024, DateTime.Now.AddDays(-5), "#333333"),
                new FileItemViewModel("Music.mp3", 600, DateTime.Now.AddDays(-1), "#222222")
            };
        }
    }

    public class FileItemViewModel : ReactiveObject
    {
        public string FileName { get; }
        public double FileSize { get; }
        public DateTime LastModified { get; }
        public string RowBackground { get; }
        public bool IsSelected { get; set; }

        public FileItemViewModel(string fileName, double fileSize, DateTime lastModified, string rowBackground)
        {
            FileName = fileName;
            FileSize = fileSize;
            LastModified = lastModified;
            RowBackground = rowBackground;
        }
    }
}
