
using System.Collections.ObjectModel;
using System.Drawing;
using ReactiveUI;
using static LorealAvaloniaUI.ViewModels.OneDriveViewModel;

namespace LorealAvaloniaUI.ViewModels
{

    public class OutlookFilesViewModel : ReactiveObject
    {
        public ObservableCollection<OutlookDisplayFiles> OutlookFiles { get; }

        
        public OutlookFilesViewModel() 
        {
            OutlookFiles = new ObservableCollection<OutlookDisplayFiles>
            {
            new OutlookDisplayFiles("OutlookFile.ost", 101, "yes", "C:/Users\\alekhya.nandina\\AppData\\Local\\Microsoft\\Outlook"),
            new OutlookDisplayFiles("OutlookFile1.pst", 143, "no", "C:\\Users\\alekhya.nandina\\OneDrive - L'Oréal\\Documents\\Outlook Files"),
            new OutlookDisplayFiles("OutlookFile2.pst", 300, "no", "C:\\Users\\alekhya.nandina\\OneDrive - L'Oréal\\Documents\\Outlook Files")
            };
        }
    }

    public class OutlookDisplayFiles: ReactiveObject
    {
        public string Filename { get; }
        public double Size { get; }
        public string AttachedToOutlook { get; }
        public string Location { get; }

        public OutlookDisplayFiles(string filename, double size, string attachedtooutlook, string location)
        {
            Filename = filename;
            Size = size;
            AttachedToOutlook = attachedtooutlook;
            Location = location;

        }
    }
}