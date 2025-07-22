using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace libomtnet.linux
{    internal class LinuxPlatform : OMTPlatform
    {
        private const int RTLD_NOW = 2; 
        private const int RTLD_GLOBAL = 8;

        [DllImport("libdl.so")]
        static extern IntPtr dlopen(string filename, int flags);

        public override IntPtr OpenLibrary(string filename)
        {
            return dlopen(filename, RTLD_GLOBAL | RTLD_NOW);
        }
        protected override string GetLibraryExtension()
        {
            return ".so";
        }
    }
}
