using System;
using System.Collections.Generic;
using System.Linq;

namespace Clipbox.Views.FilterBand.EventArgsClasses
{
    public class ApplicationInfoEventArgs : EventArgs
    {
        public ApplicationInfoEventArgs(string captionSearchString, IEnumerable<SourceApplicationInfo> selectedApplications)
        {   
            if (selectedApplications == null)
            {
                throw new ArgumentNullException(nameof(selectedApplications));
            }

            SelectedApplications = selectedApplications.ToList().AsReadOnly();
            CaptionSearchString = captionSearchString;
        }

        public IReadOnlyList<SourceApplicationInfo> SelectedApplications { get; set; }
        public string CaptionSearchString { get; set; }
    }
}
