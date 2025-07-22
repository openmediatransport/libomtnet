using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace libomtnet.win32
{
    internal class Win32Platform : OMTPlatform
    {
        enum COMPUTER_NAME_FORMAT
        {
            ComputerNameNetBIOS,
            ComputerNameDnsHostname,
            ComputerNameDnsDomain,
            ComputerNameDnsFullyQualified,
            ComputerNamePhysicalNetBIOS,
            ComputerNamePhysicalDnsHostname,
            ComputerNamePhysicalDnsDomain,
            ComputerNamePhysicalDnsFullyQualified
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string filename);

        [DllImport("kernel32.dll", CharSet=CharSet.Unicode)]
        private static extern bool GetComputerNameEx(COMPUTER_NAME_FORMAT NameType, [Out()] StringBuilder lpBuffer, ref int nSize);

        public override string GetStoragePath()
        {
           return Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + Path.DirectorySeparatorChar + "OMT";
        }
        public override string GetMachineName()
        {
            int len = 4096;
            StringBuilder sb = new StringBuilder(len);
            if (GetComputerNameEx(COMPUTER_NAME_FORMAT.ComputerNamePhysicalDnsHostname,sb,ref len))
            {
                return sb.ToString().ToUpper();
            }
            OMTLogging.Write("Unable to retrieve full hostname", "Win32Platform");
            return base.GetMachineName();
        }
        public override IntPtr OpenLibrary(string filename)
        {
            return LoadLibrary(filename);
        }

    }
}
