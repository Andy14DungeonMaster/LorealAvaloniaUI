
using System.Collections.ObjectModel;
using System;
using ReactiveUI;

namespace LorealAvaloniaUI.ViewModels
{
    public class OneDriveViewModel : ReactiveObject
    {
        public ObservableCollection<DesktopFileItemViewModel> DesktopFiles { get; }

        public OneDriveViewModel()
        {
            DesktopFiles = new ObservableCollection<DesktopFileItemViewModel>
            {
                new DesktopFileItemViewModel("Report.pdf", 750, DateTime.Now.AddDays(-2), "#222222"),
                new DesktopFileItemViewModel("Video.mp4", 1024, DateTime.Now.AddDays(-5), "#333333"),
                new DesktopFileItemViewModel("Music.mp3", 600, DateTime.Now.AddDays(-1), "#222222")
            };
        }

        public class DesktopFileItemViewModel : ReactiveObject
        {
            public string FileName { get; }
            public double FileSize { get; }
            public DateTime LastModified { get; }
            public string RowBackground { get; }
            public bool IsSelected { get; set; }

            public DesktopFileItemViewModel(string fileName, double fileSize, DateTime lastModified, string rowBackground)
            {
                FileName = fileName;
                FileSize = fileSize;
                LastModified = lastModified;
                RowBackground = rowBackground;
            }
        }
    }
}