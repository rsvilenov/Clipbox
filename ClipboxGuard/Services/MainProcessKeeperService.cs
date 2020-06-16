using ClipboxGuard.Services.EventArgsClasses;
using ClipboxGuard.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClipboxGuard.Services
{
    public class MainProcessKeeperService : IMainProcessKeeperService
    {
        public event EventHandler<RestartFatalErrorEventArgs> FatalErrorOccurred;

        public MainProcessKeeperService()
        {

        }
    }
}
