using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;

namespace libomtnet
{    internal class OMTClock : OMTBase
    {
        private long lastTimestamp = -1;
        private Stopwatch clock = Stopwatch.StartNew();
        private long clockTimestamp = -1;
        private int frameRateN = -1;
        private int frameRateD = -1;
        private int sampleRate = -1;
        private long frameInterval = -1;
        private bool audio;
        public OMTClock(bool audio)
        {
            this.audio = audio;
        }

        public void Process(ref OMTMediaFrame frame)
        {
            if (audio && frame.SampleRate != sampleRate)
            {
                Reset(frame);
            } else if ((frame.FrameRateN != frameRateN) || frame.FrameRateD != frameRateD)
            {
                Reset(frame);
            }
            if (frame.Timestamp == -1)
            {
                if (lastTimestamp == -1)
                {
                    Reset(frame);
                    frame.Timestamp = 0;
                } else
                {
                    if (audio && sampleRate > 0 && frame.SamplesPerChannel > 0)
                    {
                        frameInterval = 10000000L * frame.SamplesPerChannel;
                        frameInterval /= sampleRate;
                    }
                    frame.Timestamp = lastTimestamp + frameInterval;
                    clockTimestamp += frameInterval;

                    long diff = clockTimestamp - (clock.ElapsedMilliseconds * 10000);
                    while (diff < -frameInterval)
                    {
                        frame.Timestamp += frameInterval;
                        clockTimestamp += frameInterval;
                        diff += frameInterval;
                    }
                    while (!Exiting && (clockTimestamp > clock.ElapsedMilliseconds * 10000))
                    {
                        Thread.Sleep(1);
                    }
                }
            }
            lastTimestamp = frame.Timestamp;
        }
        private void Reset(OMTMediaFrame frame)
        {
            frameRateD = frame.FrameRateD;
            frameRateN = frame.FrameRateN;
            sampleRate = frame.SampleRate;
            if (frame.FrameRate > 0)
            {
                frameInterval = (long)(10000000 / frame.FrameRate);
            } 
            clock = Stopwatch.StartNew();
            clockTimestamp = 0;
            Debug.WriteLine("OMTClock.Reset");
        }
    }
}
