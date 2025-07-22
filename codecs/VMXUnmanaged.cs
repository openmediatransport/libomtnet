using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace libomtnet.codecs
{
    internal class VMXUnmanaged
    {
        private const string DLLPATH = @"libvmx";
        [DllImport(DLLPATH)]
        internal static extern IntPtr VMX_Create(OMTSize dimensions, VMXProfile profile, VMXColorSpace colorSpace);
        [DllImport(DLLPATH)]
        internal static extern void VMX_Destroy(IntPtr instance);
        [DllImport(DLLPATH)]
        internal static extern void VMX_SetQuality(IntPtr instance, int q);
        [DllImport(DLLPATH)]
        internal static extern void VMX_SetThreads(IntPtr instance, int t);
        [DllImport(DLLPATH)]
        internal static extern int VMX_GetThreads(IntPtr instance);
        [DllImport(DLLPATH)]
        internal static extern int VMX_LoadFrom(IntPtr instance, byte[] data, int dataLen);
        [DllImport(DLLPATH)]
        internal static extern int VMX_SaveTo(IntPtr instance, byte[] data, int maxLen);
        [DllImport(DLLPATH)]
        internal static extern int VMX_EncodeBGRA(IntPtr Instance, IntPtr src, int stride, int interlaced);
        [DllImport(DLLPATH)]
        internal static extern int VMX_EncodeBGRX(IntPtr Instance, IntPtr src, int stride, int interlaced);
        [DllImport(DLLPATH)]
        internal static extern int VMX_EncodeUYVY(IntPtr Instance, IntPtr src, int stride, int interlaced);
        [DllImport(DLLPATH)]
        internal static extern int VMX_EncodeYUY2(IntPtr Instance, IntPtr src, int stride, int interlaced);    
        [DllImport(DLLPATH)]
        internal static extern int VMX_EncodeNV12(IntPtr Instance, IntPtr srcY, int strideY, IntPtr srcUV, int strideUV, int interlaced);
        [DllImport(DLLPATH)]
        internal static extern int VMX_EncodeYV12(IntPtr Instance, IntPtr srcY, int strideY, IntPtr srcU, int strideU, IntPtr srcV, int strideV, int interlaced);
        [DllImport(DLLPATH)]
        internal static extern int VMX_DecodeUYVY(IntPtr Instance, byte[] dst, int stride);
        [DllImport(DLLPATH)]
        internal static extern int VMX_DecodeYUY2(IntPtr Instance, byte[] dst, int stride);
        [DllImport(DLLPATH)]
        internal static extern int VMX_DecodeBGRX(IntPtr Instance, byte[] dst, int stride);
        [DllImport(DLLPATH)]
        internal static extern int VMX_DecodeBGRA(IntPtr Instance, byte[] dst, int stride);
        [DllImport(DLLPATH)]
        internal static extern int VMX_DecodePreviewUYVY(IntPtr Instance, byte[] dst, int stride);
        [DllImport(DLLPATH)]
        internal static extern int VMX_DecodePreviewYUY2(IntPtr Instance, byte[] dst, int stride);
        [DllImport(DLLPATH)]
        internal static extern int VMX_DecodePreviewBGRA(IntPtr Instance, byte[] dst, int stride);
        [DllImport(DLLPATH)]
        internal static extern int VMX_DecodePreviewBGRX(IntPtr Instance, byte[] dst, int stride);
        [DllImport(DLLPATH)]
        internal static extern int VMX_GetEncodedPreviewLength(IntPtr Instance);
        [DllImport(DLLPATH)]
        internal static extern float VMX_CalculatePSNR(byte[] image1, byte[] image2, int stride, int bytesPerPixel, OMTSize sz);
    }
}
