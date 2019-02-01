using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace ClipboxSvc
{
    static class Program
    {
#pragma warning disable CS0169 // The field 'Program._svc' is never used
        static ClientProcessManagementService _svc;
#pragma warning restore CS0169 // The field 'Program._svc' is never used
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
//#if DEBUG
//            _svc = new ClientProcessManagementService();
//            _svc.OnDebug();
//#else
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new ClientProcessManagementService()
            };
            ServiceBase.Run(ServicesToRun);
//#endif
        }
    }
}
