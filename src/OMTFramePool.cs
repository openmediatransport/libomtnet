using System;
using System.Collections.Generic;
using System.Text;

namespace libomtnet
{
    internal class OMTFramePool : OMTBase
    {
        Queue<OMTFrame> pool;
        public OMTFramePool(int count, int maxDataLength, bool resizable)
        {
            pool = new Queue<OMTFrame>();
            for (int i = 0; i < count; i++) {
                pool.Enqueue(new OMTFrame(maxDataLength, resizable));
            }
        }

        protected override void DisposeInternal()
        {
            if (pool != null)
            {
                foreach (OMTFrame frame in pool)
                {
                    if (frame != null)
                    {
                        frame.Dispose();
                    }
                }
                pool.Clear();
            }
            base.DisposeInternal();
        }

        public OMTFrame Get()
        {
            lock (pool)
            {
                if (pool.Count > 0)
                {
                    return pool.Dequeue();
                }                
            }
            return null;
        }

        public void Return(OMTFrame frame)
        {
            lock (pool)
            {
                pool.Enqueue(frame);
            }
        }

        public int Count { get { lock (pool) { return pool.Count; } } }
    }
}
