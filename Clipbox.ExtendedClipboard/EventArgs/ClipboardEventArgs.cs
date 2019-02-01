using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipbox.ExtendedClipboard.EventArgs
{
    public class ClipboardEventArgs : System.EventArgs
    {
        // rather long...

        /// <summary>
        /// Cancel the propagation of the clipboard message to other programs,
        /// registered as clipboard listeners.
        /// </summary>
        public bool CancelPropagatingToOtherClipboardListeners { get; set; }
    }
}
