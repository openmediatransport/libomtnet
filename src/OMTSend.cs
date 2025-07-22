/*
* MIT License
*
* Copyright (c) 2025 Open Media Transport Contributors
*
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
*
* The above copyright notice and this permission notice shall be included in all
* copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
*
*/

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using libomtnet.codecs;
using System.Runtime.InteropServices;
using System.Threading;

namespace libomtnet
{
    public class OMTSend : OMTSendReceiveBase
    {
        private readonly OMTAddress address;
        private Socket listener;
        private OMTChannel[] channels = { };
        private object channelsLock = new object();
        private OMTDiscovery discovery;

        private OMTFrame tempVideo;
        private OMTFrame tempAudio;
        private OMTBuffer tempAudioBuffer;
        private OMTVMX1Codec codec = null;
        private OMTQuality quality = OMTQuality.Default;
        private SocketAsyncEventArgs listenEvent;

        private OMTQuality suggestedQuality = OMTQuality.Default;
        private string senderInfoXml = null;

        private OMTClock videoClock;
        private OMTClock audioClock;

        /// <summary>
        /// Create a new instance of the OMT Sender
        /// </summary>
        /// <param name="name">Specify the name of the source not including hostname</param>
        /// <param name="quality"> Specify the quality to use for video encoding. If Default, this can be automatically adjusted based on Receiver requirements.</param>
        public OMTSend(string name, OMTQuality quality)
        {
            videoClock = new OMTClock(false);
            audioClock = new OMTClock(true);
            metadataHandle = new AutoResetEvent(false);
            tallyHandle = new AutoResetEvent(false);
            listenEvent = new SocketAsyncEventArgs();
            listenEvent.Completed += OnAccept;
            tempVideo = new OMTFrame(OMTFrameType.Video, new OMTBuffer(OMTConstants.VIDEO_MIN_SIZE, true));
            tempAudio = new OMTFrame(OMTFrameType.Audio, new OMTBuffer(OMTConstants.AUDIO_MIN_SIZE, true));
            tempAudioBuffer = new OMTBuffer(OMTConstants.AUDIO_MIN_SIZE, true);
            this.discovery = OMTDiscovery.GetInstance();
            this.quality = quality;
            this.suggestedQuality = quality;
            this.listener = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);

            OMTSettings settings = OMTSettings.GetInstance();
            int startPort = settings.GetInteger("NetworkPortStart", OMTConstants.NETWORK_PORT_START);
            int endPort = settings.GetInteger("NetworkPortEnd", OMTConstants.NETWORK_PORT_END);
            for (int i = startPort; i <= endPort; i++)
            {
                try
                {
                    this.listener.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                    this.listener.Bind(new IPEndPoint(IPAddress.IPv6Any, i));
                    this.listener.Listen(5);
                    break;
                }
                catch (SocketException se)
                {
                    if (se.SocketErrorCode != SocketError.AddressAlreadyInUse | i == OMTConstants.NETWORK_PORT_END)
                    {
                        throw se;
                    }
                }
            }
            BeginAccept();
            IPEndPoint ip = (IPEndPoint)this.listener.LocalEndPoint;
            this.address = new OMTAddress(name, ip.Port);
            this.address.AddAddress(IPAddress.Loopback);
            this.discovery.RegisterAddress(address);
        }

        public override OMTStatistics GetVideoStatistics()
        {
            OMTChannel[] ch = channels;
            if (ch != null)
            {
                foreach (OMTChannel c in ch)
                {
                    if (c.IsVideo() && c.Connected)
                    {
                        OMTStatistics s = c.GetStatistics();
                        UpdateCodecTimerStatistics(ref s);
                        return s;
                    }
                }
            }
            return base.GetVideoStatistics();
        }

        public override OMTStatistics GetAudioStatistics()
        {
            OMTChannel[] ch = channels;
            if (ch != null)
            {
                foreach (OMTChannel c in ch)
                {
                    if (c.IsAudio() && c.Connected)
                    {
                        return c.GetStatistics();
                    }
                }
            }
            return base.GetAudioStatistics();
        }

        public int Port { get { return this.address.Port; } }

        /// <summary>
        /// Specify information to describe the Sender to any Receivers
        /// </summary>
        /// <param name="senderInfo"></param>
        public void SetSenderInformation(OMTSenderInfo senderInfo)
        {
            if (senderInfo == null)
            {
                this.senderInfoXml = null;
            } else
            {
                this.senderInfoXml = senderInfo.ToXML();
            } 
        }

        protected override void DisposeInternal()
        {
            if (tallyHandle != null)
            {
                tallyHandle.Set();
            }
            if (metadataHandle != null)
            {
                metadataHandle.Set();
            }
            if (videoClock != null)
            {
                videoClock.Dispose();
            }
            if (audioClock != null)
            {
                audioClock.Dispose();
            }
            lock (videoLock) { }      
            lock (audioLock) { }
            lock (metaLock) { }
            if (discovery != null)
            {
                discovery.DeregisterAddress(address);
            }
            if (listener != null)
            {
                listener.Dispose();
                listener = null;
            }
            if (listenEvent != null)
            {
                listenEvent.Completed -= OnAccept;
                listenEvent.Dispose();
                listenEvent = null;
            }
            lock (channelsLock)
            {
                if (channels != null)
                {
                    foreach (OMTChannel channel in channels)
                    {
                        if (channel != null)
                        {
                            channel.Changed -= Channel_Changed;
                            channel.Dispose();
                        }
                    }
                    channels = null;
                }
            }
            if (codec != null)
            { 
                codec.Dispose(); 
                codec = null;
            }
            discovery = null;
            OMTMetadata.FreeIntPtr(lastMetadata);
            lastMetadata = IntPtr.Zero;
            if (metadataHandle != null)
            {
                metadataHandle.Close();
                metadataHandle = null;
            }
            if (tallyHandle != null)
            {
                tallyHandle.Close();
                tallyHandle = null;
            }
            if (tempVideo != null)
            {
                tempVideo.Dispose();
                tempVideo = null;
            }
            if (tempAudio != null)
            {
                tempAudio.Dispose();
                tempAudio = null;
            }
            if (tempAudioBuffer != null)
            {
                tempAudioBuffer.Dispose();
                tempAudioBuffer = null;
            }
            base.DisposeInternal();
        }

        /// <summary>
        /// Discovery address in the format HOSTNAME (NAME)
        /// </summary>
        public string Address { get { return address.ToString(); } }

        /// <summary>
        /// Direct connection address in the format omt://hostname:port
        /// </summary>
        public string URL { get { return address.ToURL(); } }

        public int Connections { get { 
                
                OMTChannel[] ch = channels;
                if (ch != null)
                {
                    return ch.Length;
                }
                return 0;
            
            } }

        private void OnAccept(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                if (e.SocketError == SocketError.Success)
                {
                    Socket socket = null;
                    OMTChannel channel = null;
                    try
                    {
                        socket = e.AcceptSocket;
                        channel = new OMTChannel(socket, OMTFrameType.Metadata, null, metadataHandle);
                        channel.StartReceive();
                        if (senderInfoXml != null)
                        {
                            channel.Send(new OMTMetadata(0, senderInfoXml));
                        }
                        channel.Send(OMTMetadata.FromTally(lastTally));
                        OMTLogging.Write("AddConnection: " + socket.RemoteEndPoint.ToString(), "OMTSend.BeginAccept");
                        AddChannel(channel);
                    }
                    catch (Exception ex)
                    {
                        OMTLogging.Write(ex.ToString(), "OMTSend.BeginAccept");
                        if (channel != null)
                        {
                            channel.Changed -= Channel_Changed;
                            channel.Dispose();
                        }
                        if (socket != null)
                        {
                            socket.Dispose();
                        }
                    }
                    BeginAccept();
                }
            }
            catch (Exception ex)
            {
                OMTLogging.Write(ex.ToString(), "OMTSend.OnAccept");
            }            
        }
        private void BeginAccept()
        {
            Socket listener = this.listener;
            if (listener != null)
            {
                listenEvent.AcceptSocket = null;
                if (this.listener.AcceptAsync(listenEvent) == false) {
                    OnAccept(this.listener, listenEvent);
                }
            }                
        }
        internal void AddChannel(OMTChannel channel)
        {
            lock (channelsLock)
            {
                List<OMTChannel> list = new List<OMTChannel>();
                list.AddRange(channels);
                list.Add(channel);
                channels = list.ToArray();
            }
            channel.Changed += Channel_Changed;
            UpdateTally();
        }
        internal void RemoveChannel(OMTChannel channel)
        {
            lock (channelsLock)
            {
                if (channel != null)
                {
                    List<OMTChannel> list = new List<OMTChannel>();
                    list.AddRange(channels);
                    list.Remove(channel);
                    channels = list.ToArray();
                    channel.Changed -= Channel_Changed;
                    channel.Dispose();
                }
            }
            OMTLogging.Write("RemoveConnection", "OMTSend.RemoveChannel");
        }

        internal override void OnTallyChanged(OMTTally tally)
        {
            SendMetadata(OMTMetadata.FromTally(tally));
        }
        internal int Send(OMTFrame frame)
        {
            int len = 0;
            OMTQuality suggested = OMTQuality.Default;
            OMTChannel[] channels = this.channels;
            bool removed = false;
            if (channels != null)
            {
                for (int i = 0; i < channels.Length; i++)
                {
                    if (channels[i].Connected)
                    {
                        len += channels[i].Send(frame);
                        if (channels[i].IsVideo())
                        {
                            if (channels[i].SuggestedQuality > suggested)
                            {
                                suggested = channels[i].SuggestedQuality;
                            }
                        }
                    }
                    else
                    {
                        RemoveChannel(channels[i]);
                        removed = true;
                    }
                }
                if (removed)
                {
                    UpdateTally();
                }
                if (quality == OMTQuality.Default)
                {
                    suggestedQuality = suggested;
                }
            }
            return len;
        }
        private void CreateCodec(int width, int height, int framesPerSecond, VMXColorSpace colorSpace)
        {
            VMXProfile prof = VMXProfile.Default;
            if (suggestedQuality != OMTQuality.Default)
            {
                if (suggestedQuality >= OMTQuality.Low) prof = VMXProfile.OMT_LQ;
                if (suggestedQuality >= OMTQuality.Medium) prof = VMXProfile.OMT_SQ;
                if (suggestedQuality >= OMTQuality.High) prof = VMXProfile.OMT_HQ;
            }
            if (codec == null)
            {
                codec = new OMTVMX1Codec(width, height, framesPerSecond, prof, colorSpace);
            }
            else if (codec.Width != width || codec.Height != height || codec.Profile != prof || codec.ColorSpace != colorSpace || codec.FramesPerSecond != framesPerSecond)
            {
                codec.Dispose();
                codec = new OMTVMX1Codec(width, height, framesPerSecond, prof, colorSpace);
            }
        }

        /// <summary>
        /// Receive any available metadata in the buffer, or wait for metadata if empty
        /// Returns true if metadata was found, false of timed out
        /// </summary>
        /// <param name="millisecondsTimeout">The maximum time to wait for a new frame if empty</param>
        /// <param name="outFrame">The frame struct to fill with the received data</param>
        public bool Receive(int millisecondsTimeout, ref OMTMediaFrame outFrame)
        {
            lock (metaLock)
            {
                if (Exiting) return false;
                if (ReceiveInternal(ref outFrame)) return true;
                for (int i = 0; i < 2; i++)
                {
                    if (metadataHandle.WaitOne(millisecondsTimeout) == false) return false;
                    if (Exiting) return false;
                    if (ReceiveInternal(ref outFrame)) return true;
                }              
            }
            return false;
        }

        private bool ReceiveInternal(ref OMTMediaFrame frame)
        {
            OMTChannel[] channels = this.channels;
            for (int i = 0; i < channels.Length; i++)
            {
                OMTChannel ch = channels[i];
                if (ch != null)
                {
                    if (ch.ReadyMetadataCount > 0)
                    {
                        OMTMetadata m = ch.ReceiveMetadata();
                        return ReceiveMetadata(m, ref frame);
                    }
                }
            }
            return false;
        }
        internal override OMTTally GetTallyInternal()
        {
            OMTTally tally = new OMTTally();
            OMTChannel[] channels = this.channels;
            if (channels != null)
            {
                for (int i = 0; i < channels.Length; i++)
                {
                    OMTTally t = channels[i].GetTally();
                    if (t.Program == 1) tally.Program = 1;
                    if (t.Preview == 1) tally.Preview = 1;
                }
            }
            return tally;
        }

        /// <summary>
        /// Send a frame to any receivers currently connected. 
        /// Video: Supports UYVY, YUY2, RGBA or RGBX frames
        /// Audio: Supports planar 32bit floating point audio
        /// Metadata: Supports UTF8 encoded XML 
        /// </summary>
        /// <param name="frame">The frame to send</param>
        public int Send(OMTMediaFrame frame)
        {
            if (Exiting) return 0;
            if (frame.Type == OMTFrameType.Video)
            {
                return SendVideo(frame);
            }
            else if (frame.Type == OMTFrameType.Audio)
            {
                return SendAudio(frame);
            }
            else if (frame.Type == OMTFrameType.Metadata)
            {
                return SendMetadata(frame);
            }
            return 0;
        }

        private int SendMetadata(OMTMediaFrame metadata)
        {
            OMTMetadata m = OMTMetadata.FromMediaFrame(metadata);
            if (m != null)
            {
                return SendMetadata(m);
            }
            return 0;
        }

        private int SendMetadata(OMTMetadata metadata)
        {
            lock (metaLock)
            {
                if (Exiting) return 0;
                int len = 0;
                OMTChannel[] channels = this.channels;
                if (channels != null)
                {
                    for (int i = 0; i < channels.Length; i++)
                    {
                        OMTChannel ch = channels[i];
                        if (ch.IsMetadata())
                        {
                            len += channels[i].Send(metadata);
                        }
                    }
                }
                return len;
            }
        }

        private int SendVideo(OMTMediaFrame frame)
        {
            lock (videoLock)
            {
                if (Exiting) return 0;
                if (frame.Data != IntPtr.Zero && frame.DataLength > 0)
                {
                    tempVideo.Data.Resize(frame.DataLength);

                    if ((frame.Codec == (int)OMTCodec.UYVY) || (frame.Codec == (int)OMTCodec.BGRA) ||
                        (frame.Codec == (int)OMTCodec.YUY2) || (frame.Codec == (int)OMTCodec.NV12) || (frame.Codec == (int)OMTCodec.YV12))
                    {
                        if (frame.Width >= 16 && frame.Height >= 16 && frame.Stride >= frame.Width)
                        {
                            bool interlaced = frame.Flags.HasFlag(OMTVideoFlags.Interlaced);
                            bool alpha = frame.Flags.HasFlag(OMTVideoFlags.Alpha);

                            CreateCodec(frame.Width, frame.Height, (int)frame.FrameRate, (VMXColorSpace)frame.ColorSpace);
                            byte[] buffer = tempVideo.Data.Buffer;
                            int len;
                            BeginCodecTimer();
                            VMXImageType itype = VMXImageType.None;
                            if (frame.Codec == (int)OMTCodec.UYVY)
                            {
                                itype = VMXImageType.UYVY;                                
                            }
                            else if (frame.Codec == (int)OMTCodec.YUY2)
                            {
                                itype = VMXImageType.YUY2;
                            }
                            else if (frame.Codec == (int)OMTCodec.NV12)
                            {
                                itype = VMXImageType.NV12;
                            } else if (frame.Codec == (int)OMTCodec.YV12)
                            {
                                itype = VMXImageType.YV12;
                            }
                            else if (frame.Codec == (int)OMTCodec.BGRA)
                            {
                                if (alpha)
                                {
                                    itype = VMXImageType.BGRA;
                                } else
                                {
                                    itype = VMXImageType.BGRX;
                                } 
                            }
                            len = codec.Encode(itype, frame.Data, frame.Stride, buffer, interlaced);
                            EndCodecTimer();
                            if (len > 0)
                            {
                                tempVideo.SetDataLength(len);
                                tempVideo.SetPreviewDataLength(codec.GetEncodedPreviewLength());
                                tempVideo.ConfigureVideo((int)OMTCodec.VMX1, frame.Width, frame.Height, frame.FrameRateN, frame.FrameRateD, frame.AspectRatio, frame.Flags, frame.ColorSpace);
                                videoClock.Process(ref frame);
                                tempVideo.Timestamp = frame.Timestamp;
                                return Send(tempVideo);
                            }
                            else
                            {
                                OMTLogging.Write("Encoding failed at timestamp: " + frame.Timestamp, "OMTSend.SendVideo");
                            }

                        }
                        else
                        {
                            OMTLogging.Write("Frame dimensions invalid: " + frame.Width + "x" + frame.Height + " Stride: " + frame.Stride, "OMTSend.SendVideo");
                        }
                    } else if (frame.Codec == (int)OMTCodec.VMX1)
                    {
                        if (frame.DataLength > 0)
                        {
                            tempVideo.SetDataLength(frame.DataLength);
                            tempVideo.SetPreviewDataLength(frame.DataLength);
                            Marshal.Copy(frame.Data, tempVideo.Data.Buffer, 0, frame.DataLength);
                            tempVideo.ConfigureVideo((int)OMTCodec.VMX1, frame.Width, frame.Height, frame.FrameRateN, frame.FrameRateD, frame.AspectRatio, frame.Flags, frame.ColorSpace);
                            videoClock.Process(ref frame);
                            tempVideo.Timestamp = frame.Timestamp;
                            return Send(tempVideo);
                        } else
                        {
                            OMTLogging.Write("Frame DataLength invalid", "OMTSend.SendVideo");
                        }
                    }
                    else
                    {
                        OMTLogging.Write("Codec not supported: " + frame.Codec, "OMTSend.SendVideo");
                    }
                }
            }
            return 0;
        }
        private int SendAudio(OMTMediaFrame frame)
        {
            lock (audioLock)
            {
                if (Exiting) return 0;
                if (frame.Data != IntPtr.Zero && frame.DataLength > 0)
                {
                    if (frame.DataLength > OMTConstants.AUDIO_MAX_SIZE)
                    {
                        OMTLogging.Write("Audio DataLength exceeded maximum: " + frame.DataLength, "OMTSend");
                        return 0;
                    }
                    tempAudioBuffer.Resize(frame.DataLength);
                    tempAudio.Data.Resize(frame.DataLength);
                    Marshal.Copy(frame.Data, tempAudioBuffer.Buffer, 0, frame.DataLength);
                    tempAudioBuffer.SetBuffer(0, frame.DataLength);
                    tempAudio.Data.SetBuffer(0, 0);
                    OMTActiveAudioChannels ch = OMTFPA1Codec.Encode(tempAudioBuffer, frame.Channels, frame.SamplesPerChannel, tempAudio.Data);
                    tempAudio.SetDataLength(tempAudio.Data.Length);
                    tempAudio.ConfigureAudio(frame.SampleRate, frame.Channels, frame.SamplesPerChannel, ch);
                    audioClock.Process(ref frame);
                    tempAudio.Timestamp = frame.Timestamp;
                    return Send(tempAudio);
                }
            }
            return 0;
        }

    }
}
