using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace libomtnet
{
    public class OMTBase : IDisposable
    {
        private bool disposedValue;
        private bool exiting;
        protected virtual void DisposeInternal()
        {            
        }        
        protected bool Exiting { get { return exiting; } }
        protected void SetExiting()
        {
            exiting = true;
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                exiting = true;
                if (disposing)
                {
                    DisposeInternal();
                }
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
