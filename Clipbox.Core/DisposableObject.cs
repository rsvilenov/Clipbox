using System;

namespace Clipbox.Core
{
    public abstract class DisposableObject : IDisposable
    {

        #region IDisposable Support
        protected virtual void Dispose(bool disposing)
        {
        }

         ~DisposableObject()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
