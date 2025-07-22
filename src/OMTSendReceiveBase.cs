using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace libomtnet
{
    public class OMTSendReceiveBase : OMTBase
    {
        protected object videoLock = new object();
        protected object audioLock = new object();
        protected object metaLock = new object();
        protected AutoResetEvent metadataHandle;
        protected AutoResetEvent tallyHandle;
        protected IntPtr lastMetadata;
        protected OMTTally lastTally = new OMTTally();

        private Stopwatch timer = Stopwatch.StartNew();
        private long codecTime = 0;
        private long codecTimeSinceLast = 0;
        private long codecStartTime = 0;


        /// <summary>
        /// Receives the current tally state across all connections to a Sender.
        /// If this function times out, the last known tally state will be received.
        /// </summary>
        /// <param name="millisecondsTimeout">milliseconds to wait for tally change. set to 0 to receive current tally</param>
        /// <param name="tally"></param>
        /// <returns></returns>
        public bool GetTally(int millisecondsTimeout, ref OMTTally tally)
        {
            if (Exiting) return false;
            if (millisecondsTimeout > 0)
            {
                if (tallyHandle != null)
                {
                    if (tallyHandle.WaitOne(millisecondsTimeout))
                    {
                        tally = lastTally;
                        return true;
                    }
                }
            }
            tally = lastTally;
            return false;
        }

        internal virtual OMTTally GetTallyInternal()
        {
            return new OMTTally();
        }

        internal void Channel_Changed(object sender, OMTEventArgs e)
        {
            if (e.Type == OMTEventType.TallyChanged)
            {
                UpdateTally();
            }
        }

        internal virtual void OnTallyChanged( OMTTally tally)
        {
        }

        internal void UpdateTally()
        {
            OMTTally tally = GetTallyInternal();
            if (tally.Preview != lastTally.Preview || tally.Program != lastTally.Program)
            {
                lastTally = tally;
                OnTallyChanged(lastTally);
                if (tallyHandle != null)
                {
                    tallyHandle.Set();
                }
            }
        }

        public virtual OMTStatistics GetVideoStatistics()
        {
            return new OMTStatistics();
        }
        public virtual OMTStatistics GetAudioStatistics()
        {
            return new OMTStatistics();
        }
        internal void BeginCodecTimer()
        {
            codecStartTime = timer.ElapsedMilliseconds;
        }
        internal void EndCodecTimer()
        {
            long v = (timer.ElapsedMilliseconds - codecStartTime);
            codecTime += v;
            codecTimeSinceLast += v;
        }
        internal void UpdateCodecTimerStatistics(ref OMTStatistics v)
        {
            v.CodecTime = codecTime;
            v.CodecTimeSinceLast = codecTimeSinceLast;
            codecTimeSinceLast = 0;
        }
        internal bool ReceiveMetadata(OMTMetadata frame, ref OMTMediaFrame outFrame)
        {
            lock (metaLock)
            {
                if (Exiting) return false;
                OMTMetadata.FreeIntPtr(lastMetadata);
                lastMetadata = IntPtr.Zero;
                outFrame.Type = OMTFrameType.Metadata;
                outFrame.Timestamp = frame.Timestamp;
                outFrame.Data = frame.ToIntPtr(ref outFrame.DataLength);
                lastMetadata = outFrame.Data;
                return true;
            }
        }

        protected override void DisposeInternal()
        {
            OMTMetadata.FreeIntPtr(lastMetadata);
            lastMetadata = IntPtr.Zero;
            base.DisposeInternal();
        }
    }
}
