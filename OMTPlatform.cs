using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Net;
using System.Runtime.InteropServices;
using System.Reflection;

namespace libomtnet
{
    public class OMTPlatform
    {
        private static OMTPlatform instance;
        private static object globalLock = new object();
        private static OMTPlatformType platformType;

        static OMTPlatform()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                platformType = OMTPlatformType.Win32;
            }
            else
            {
                platformType = OMTPlatformType.Linux;
                string path = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                if (path.Contains("/Containers/Data/Application/"))
                {
                    platformType = OMTPlatformType.iOS;
                } else if (Directory.Exists("/System/Applications/Utilities/Terminal.app"))
                {
                    platformType = OMTPlatformType.MacOS;
                }                    
            }
        }

        protected virtual string GetLibraryExtension()
        {
            return ".dll";
        }

        public virtual string GetMachineName()
        {
            return Environment.MachineName.ToUpper();
        }

        public virtual IntPtr OpenLibrary(string filename)
        {
            return IntPtr.Zero;
        }

        public virtual string GetStoragePath()
        {
           return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + Path.DirectorySeparatorChar + "OMT";
        }
        public static OMTPlatformType GetPlatformType()
        {
            return platformType;
        }
        public static OMTPlatform GetInstance()
        {
            lock (globalLock)
            {
                if (instance == null)
                {
                    switch (GetPlatformType())
                    {
                        case OMTPlatformType.Win32:
                            instance = new win32.Win32Platform();
                            break;
                        case OMTPlatformType.MacOS:
                        case OMTPlatformType.iOS:
                            instance = new mac.MacPlatform();
                            break;
                        case OMTPlatformType.Linux:
                            instance = new linux.LinuxPlatform();
                            break;
                        default:
                            instance = new OMTPlatform();
                            break;
                    }
                }
                return instance;
            }
        }
    }

}
