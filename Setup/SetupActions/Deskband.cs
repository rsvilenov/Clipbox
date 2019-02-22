using Setup.SetupActions.Interfaces;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace Setup.SetupActions
{
    public class Deskband : ISetupAction
    {
        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        private string regSvrPath;

        public Deskband()
        {
            regSvrPath = Path.Combine(Environment.SystemDirectory, "regsvr32");
        }

        public void Do()
        {
            if (!File.Exists(regSvrPath))
            {
                throw new KnownSetupException($"RegSvr32 was not found in {regSvrPath}");
            }

            using (Process p = new Process())
            {
                p.StartInfo = new ProcessStartInfo(regSvrPath);
                p.StartInfo.Arguments = "/s {}";
            }
        }

        public void Undo()
        {
            throw new NotImplementedException();
        }
    }
}
