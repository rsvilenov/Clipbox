using System;

namespace Clipbox.Views.FilterBand.EventArgsClasses
{
    public class ApplicationInfoEventArgs : EventArgs
    {
        public ApplicationInfoEventArgs(string selectedApplicationExeName, string captionSearchString)
        {
            SelectedApplicationExeName = selectedApplicationExeName;
            CaptionSearchString = captionSearchString;
        }

        public string SelectedApplicationExeName { get; set; }
        public string CaptionSearchString { get; set; }
    }
}
