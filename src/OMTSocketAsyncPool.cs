using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

namespace libomtnet
{
    internal class OMTSocketAsyncPool : OMTBase
    {
        private Queue<SocketAsyncEventArgs> pool;
        private int bufferSize;
        private object lockSync = new object(); 

        protected virtual void OnCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                OMTLogging.Write("Socket Pool Error: " + e.SocketError.ToString() + "," + e.BytesTransferred, "OMTSocketAsyncPool");
            }
            ReturnEventArgs(e);
        }

        public object SyncObject { get { return lockSync; } }

        public OMTSocketAsyncPool(int count, int bufferSize)
        {
            this.bufferSize = bufferSize;
            pool = new Queue<SocketAsyncEventArgs>();
            for (int i = 0; i < count; i++)
            {
                SocketAsyncEventArgs e = new SocketAsyncEventArgs();
                if (bufferSize > 0)
                {
                    byte[] buf = new byte[bufferSize];
                    e.SetBuffer(buf,0,buf.Length);
                }
                e.Completed += OnCompleted;
                pool.Enqueue(e);
            }
        }

        public void Resize(SocketAsyncEventArgs e, int length)
        {
            if (e != null)
            {
                if (e.Buffer.Length < length)
                {
                    byte[] buf = new byte[length];
                    e.SetBuffer(buf, 0, buf.Length);
                    Debug.WriteLine("SocketPool.Resize: " + length);
                }
            }
        }

        public void SendAsync(Socket socket, SocketAsyncEventArgs e)
        {
            lock (lockSync)
            {
                if (socket != null)
                {
                    if (socket.SendAsync(e) == false)
                    {
                        OnCompleted(this, e);
                    }
                }
            }
        }

        internal SocketAsyncEventArgs GetEventArgs()
        {
            lock (lockSync)
            {
                if (pool == null) return null;
                if (pool.Count > 0) {
                   SocketAsyncEventArgs e = pool.Dequeue();
                   e.SetBuffer(0, e.Buffer.Length);
                   return e;
                } 
            }
            return null;
        }

        public int GetAvailableBufferSize()
        {
            lock (lockSync)
            {
                if (pool == null) return 0;
                return pool.Count * this.bufferSize;
            }
        }

        public int Count { get { lock (pool)  { return pool.Count; } } }

        internal void ReturnEventArgs(SocketAsyncEventArgs e)
        {
            lock (lockSync)
            {
                if (pool == null)
                {
                    e.Dispose();
                } else
                {
                    pool.Enqueue(e);
                }
            }
        }

        protected override void DisposeInternal()
        {
            lock (lockSync)
            {
                if (pool != null)
                {
                    while (pool.Count > 0)
                    {
                        SocketAsyncEventArgs e = pool.Dequeue();
                        if (e != null)
                        {
                            e.Completed -= OnCompleted;
                            e.Dispose();
                        }
                    }
                    pool.Clear();
                    pool = null;
                }
            }
            base.DisposeInternal();
        }
    }
}
