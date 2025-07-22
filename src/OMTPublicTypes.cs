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
using System.Runtime.InteropServices;
using System.Xml;
using System.IO;

namespace libomtnet
{
    [Flags]
    public enum OMTFrameType
    {
        None = 0,
        Metadata = 1,
        Video = 2,
        Audio = 4
    }

    /// <summary>
    /// Flags set on video frames
    /// Interlaced: Frames are interlaced
    /// Alpha: Frames contain an alpha channel
    /// PreMultiplied: When combined with Alpha, alpha channel is premultiplied, otherwise straight
    /// Preview: Frame is a special 1/8th preview frame
    /// </summary>
    [Flags]
    public enum OMTVideoFlags
    {
        None = 0,
        Interlaced = 1,
        Alpha = 2,
        PreMultiplied = 4,
        Preview = 8
    }

    /// <summary>
    /// Supported Codecs:
    /// VMX1 = Fast video codec
    /// UYVY = 16bpp YUV format
    /// YUY2 = 16bpp YUV format YUYV pixel order
    /// BGRA = 32bpp RGBA format (Same as ARGB32 on Win32)
    /// FPA1 = Floating-point Planar Audio 32bit
    /// </summary>
    public enum OMTCodec
    {
        VMX1 = 0x31584D56,
        FPA1 = 0x31415046, //Planar audio
        UYVY = 0x59565955,
        YUY2 = 0x32595559,
        BGRA = 0x41524742,
        NV12 = 0x3231564E,
        YV12 = 0x32315659
    }

    public enum OMTPlatformType
    {
        Unknown = 0,
        Win32 = 1,
        MacOS = 2,
        Linux = 3,
        iOS = 4
    }

    public enum OMTColorSpace
    {
        Undefined = 0,
        BT601 = 601,
        BT709 = 709
    }

    /// <summary>
    /// Specify the preferred uncompressed video format of decoded frames
    /// UYVY is always the fastest, if no alpha channel is required.
    /// UYVYorBGRA will provide BGRA only when alpha channel is present.
    /// BGRA will always convert back to BGRA
    /// </summary>
    public enum OMTPreferredVideoFormat
    {
        UYVY = 0,
        UYVYorBGRA = 1,
        BGRA = 2
    }

    /// <summary>
    /// Flags to enable certain features on a Receiver
    /// Preview: Receive only a 1/8th preview of the video.
    /// IncludeCompressed: Include a copy of the compressed VMX video frames for further processing or recording.
    /// </summary>
    [Flags]
    public enum OMTReceiveFlags
    {
        None = 0,
        Preview = 1,
        IncludeCompressed = 2
    }

    /// <summary>
    /// Specify the video encoding quality.
    /// If set to Default, the Sender is configured to allow suggestions from all Receivers.
    /// The highest suggestion amongst all receivers is then selected.
    /// If a Receiver is set to Default, then it will defer the quality to whatever is set amongst other Receivers.
    /// </summary>
    public enum OMTQuality
    {
        Default = 0,
        Low = 1,
        Medium = 50,
        High = 100
    }

    public struct OMTStatistics
    {
        public long BytesSent;
        public long BytesReceived;
        public long BytesSentSinceLast;
        public long BytesReceivedSinceLast;

        public long Frames;
        public long FramesSinceLast;
        public long FramesDropped;

        public long CodecTime;
        public long CodecTimeSinceLast;

        public void ToIntPtr(IntPtr ptr)
        {
            if (ptr != IntPtr.Zero)
            {
                Marshal.WriteInt64(ptr, BytesSent);
                Marshal.WriteInt64(ptr + 8, BytesReceived);
                Marshal.WriteInt64(ptr + 16, BytesSentSinceLast);
                Marshal.WriteInt64(ptr + 24, BytesReceivedSinceLast);
                Marshal.WriteInt64(ptr + 32, Frames);
                Marshal.WriteInt64(ptr + 40, FramesSinceLast);
                Marshal.WriteInt64(ptr + 48, FramesDropped);
                Marshal.WriteInt64(ptr + 56, CodecTime);
                Marshal.WriteInt64(ptr + 64, CodecTimeSinceLast);
            }
        }
    }

    public class OMTSenderInfo
    {
        public string ProductName;
        public string Manufacturer;
        public string Version;

        public OMTSenderInfo() { }
        public OMTSenderInfo(string productName, string manufacturer, string version)
        {
            ProductName = productName;
            Manufacturer = manufacturer;
            Version = version;
        }

        public string ToXML()
        {
            StringWriter sw = new StringWriter();
            XmlTextWriter t = new XmlTextWriter(sw);
            t.Formatting = Formatting.Indented;
            t.WriteStartElement(OMTMetadataTemplates.SENDER_INFO_NAME);
            t.WriteAttributeString("ProductName", ProductName);
            t.WriteAttributeString("Manufacturer", Manufacturer);
            t.WriteAttributeString("Version", Version);
            t.WriteEndElement();
            t.Close();
            return sw.ToString();
        }
        public static OMTSenderInfo FromXML(string xml)
        {
            XmlDocument doc = OMTMetadataUtils.TryParse(xml);
            if (doc != null)
            {
                XmlNode e = doc.DocumentElement;
                if (e != null)
                {
                    OMTSenderInfo senderInfo = new OMTSenderInfo();
                    XmlNode a = e.Attributes.GetNamedItem("ProductName");
                    if (a != null) senderInfo.ProductName = a.InnerText;
                    a = e.Attributes.GetNamedItem("Manufacturer");
                    if (a != null) senderInfo.Manufacturer = a.InnerText;
                    a = e.Attributes.GetNamedItem("Version");
                    if (a != null) senderInfo.Version = a.InnerText;
                    return senderInfo;
                }
            }
            return null;
        }
    }

    /// <summary>
    /// Stores one frame of Video, Audio or Metadata
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct OMTMediaFrame
    {
        public OMTFrameType Type;

        /// <summary>
        /// This is a timestamp where 1 second = 10,000,000
        /// A special value of -1 can be specified to tell the Sender to generate timestamps and throttle as required to maintain
        /// the specified FrameRate or SampleRate of the frame.
        /// </summary>
        public long Timestamp;

        /// <summary>
        /// Sending:
        ///     Video: 'UYVY', 'YUY2', 'NV12', 'YV12, 'BGRA', 'VMX1' are supported (BGRA will be treated as BGRX where alpha flags are not set)
        ///     Audio: Only 'FPA1' is supported (32bit floating point planar audio)
        /// Receiving:
        ///     Video: Only 'UYVY', 'BGRA' and 'BGRX' are supported
        ///     Audio: Only 'FPA1' is supported (32bit floating point planar audio)
        /// </summary>
        public int Codec;

        //Video Properties
        public int Width;
        public int Height;
        public int Stride;
        public OMTVideoFlags Flags;

        /// <summary>
        /// Frame Rate Numerator/Denominator in Frames Per Second, for example Numerator 60 and Denominator 1 is 60 frames per second.
        /// </summary>
        public int FrameRateN;
        public int FrameRateD;

        /// <summary>
        /// Display aspect ratio expressed as a ratio of width/height. For example 1.777777777777778 for 16/9
        /// </summary>
        public float AspectRatio;

        /// <summary>
        /// Color space of the frame. If undefined a height < 720 is BT601 and everything else BT709
        /// </summary>
        public OMTColorSpace ColorSpace;

        //Audio Properties
        public int SampleRate;
        public int Channels;
        public int SamplesPerChannel;

        //Data Properties
        
        /// <summary>
        /// Video: Uncompressed pixel data
        /// Audio: Planar 32bit floating point audio
        /// Metadata: UTF-8 encoded XML string with terminating null character
        /// </summary>
        public IntPtr Data;

        /// <summary>
        /// Video: Number of bytes total including stride
        /// Audio: Number of bytes (SamplesPerChannel * Channels * 4)
        /// Metadata: Number of bytes in UTF-8 encoded string + 1 for terminating null character. 
        /// </summary>
        public int DataLength;

        /// <summary>
        /// Receive only. Use standard Data/DataLength if sending VMX1 frames with a Sender
        /// If IncludeCompressed OMTReceiveFlags is set, this will include the original compressed video frame in VMX1 format.
        /// This could then be muxed into an AVI or MOV file using FFmpeg or similar APIs
        /// </summary>
        public IntPtr CompressedData;
        public int CompressedLength;

        public static IntPtr ToIntPtr(OMTMediaFrame frame)
        {
            IntPtr dst = Marshal.AllocHGlobal(Marshal.SizeOf(frame));
            Marshal.StructureToPtr(frame, dst, false);
            return dst;
        }
        public static void FreeIntPtr(IntPtr ptr)
        {
            if (ptr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
        public static OMTMediaFrame FromIntPtr(IntPtr ptr)
        {
           return (OMTMediaFrame)Marshal.PtrToStructure(ptr, typeof(OMTMediaFrame));
        }

        public float FrameRate { get {
                return OMTUtils.ToFrameRate(FrameRateN, FrameRateD);
            } 
            set
            {
                OMTUtils.FromFrameRate(value,ref FrameRateN,ref FrameRateD);
            }
        }

    }

    public struct OMTSize
    {
        public int Width;
        public int Height;
        public OMTSize(int width, int height)
        {
            Width = width;
            Height = height;
        }
    }

    public struct OMTTally
    {
        public int Preview;
        public int Program;
        public OMTTally(int preview, int program)
        {
            this.Preview = preview;
            this.Program = program;
        }

        public override string ToString()
        {
            return "Preview: " + Preview + " Program: " + Program;
        }
    }

}
