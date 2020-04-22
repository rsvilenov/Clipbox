using System;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

namespace Clipbox.Infrastructure.Threading
{
    public class GlobalNamedMutex : IDisposable
    {
        // this is a modified version of deepee1's answer in this thread:
        // https://stackoverflow.com/questions/229565/what-is-a-good-pattern-for-using-a-global-mutex-in-c
        
            
        //edit by user "jitbit" - renamed private fields to "_"
        public bool _hasHandle = false;
        Mutex _mutex;

        private void InitMutex(string mutexName)
        {
            string mutexId = string.Format("Global\\{{{0}}}", mutexName);
            _mutex = new Mutex(false, mutexId);

            var allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow);
            var securitySettings = new MutexSecurity();
            securitySettings.AddAccessRule(allowEveryoneRule);
            _mutex.SetAccessControl(securitySettings);
        }

        public GlobalNamedMutex(string mutexName, int timeoutMs)
        {
            InitMutex(mutexName);

            try
            {
                if (timeoutMs < 0)
                    _hasHandle = _mutex.WaitOne(Timeout.Infinite, false);
                else
                    _hasHandle = _mutex.WaitOne(timeoutMs, false);

                if (_hasHandle == false)
                    throw new TimeoutException("Timeout waiting for exclusive access on SingleInstance");
            }
            catch (AbandonedMutexException)
            {
                _hasHandle = true;
            }
        }


        public void Dispose()
        {
            if (_mutex != null)
            {
                if (_hasHandle)
                    _mutex.ReleaseMutex();
                _mutex.Close();
            }
        }
    }
}
