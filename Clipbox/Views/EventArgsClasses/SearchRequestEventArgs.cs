using System;

namespace Clipbox.Views.EventArgsClasses
{
    public class SearchRequestEventArgs : EventArgs
    {
        public string SearchString { get; }

        public SearchRequestEventArgs(string searchString)
        {
            SearchString = searchString;
        }
    }
}
