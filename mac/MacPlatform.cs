using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace libomtnet.mac
{
    internal class MacPlatform : OMTPlatform
    {
        private const int RTLD_NOW = 2;
        private const int RTLD_GLOBAL = 8;

        [DllImport("libdl.dylib")]
        static extern IntPtr dlopen(string filename, int flags);
        public override string GetStoragePath()
        {
            return base.GetStoragePath();
        }
        public override IntPtr OpenLibrary(string filename)
        {
            return dlopen(filename, RTLD_NOW | RTLD_GLOBAL);
        }

        protected override string GetLibraryExtension()
        {
            return ".dylib";
        }
    }
}
