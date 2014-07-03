using System;

namespace Bitlet.Coinbase.Utilities
{
    public abstract class DisposableObject : IDisposable
    {
        public bool Disposed { get; private set; }

        ~DisposableObject()
        {
            Dispose(false);
        }

        void IDisposable.Dispose()
        {
            if (!Disposed)
            {
                Dispose(true);
                GC.SuppressFinalize(this);
                Disposed = true;
            }
        }

        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeManagedResources();
            }

            DisposeUnmanagedResources();
        }

        protected abstract void DisposeManagedResources();

        protected virtual void DisposeUnmanagedResources() { }
    }
}
