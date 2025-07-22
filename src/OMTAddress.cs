using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace libomtnet
{
    public class OMTAddress
    {
        private string name;
        private readonly string machineName;
        private readonly int port;
        private IPAddress[] addresses = { };
        private DateTime expiry = DateTime.MinValue;
        private const int MAX_FULLNAME_LENGTH = 63;

        public OMTAddress(string name, int port)
        {
            this.name = SanitizeName(name);
            this.port = port;
            this.machineName = SanitizeName(OMTPlatform.GetInstance().GetMachineName());
            this.addresses = new IPAddress[] { };
            LimitNameLength();
        }
        public OMTAddress(string machineName, string name, int port)
        {
            this.name = SanitizeName(name);
            this.port = port;
            this.machineName = SanitizeName(machineName);
            this.addresses = new IPAddress[] { };
            LimitNameLength();
        }

        public string ToURL()
        {
            return OMTConstants.URL_PREFIX + this.machineName + ":" + port; 
        }

        private void LimitNameLength()
        {
            int oversize = ToString().Length - MAX_FULLNAME_LENGTH;
            if (oversize > 0)  
            {
                if (oversize < this.name.Length)
                {
                    OMTLogging.Write("TruncatedNameBefore: " + this.name, "OMTAddress");
                    this.name = this.name.Substring(0, this.name.Length - oversize).Trim();
                    OMTLogging.Write("TruncatedName: " + this.name, "OMTAddress");
                }
            }
        }

        public bool AddAddress(IPAddress address)
        {
            if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                byte[] b = address.GetAddressBytes();
                byte[] b128 = new byte[16];

                b128[10] = 0xFF;
                b128[11] = 0xFF;

                b128[12] = b[0];
                b128[13] = b[1];
                b128[14] = b[2];
                b128[15] = b[3];

                address = new IPAddress(b128);
            }
            if (!HasAddress(address))
            {
                List<IPAddress> list = new List<IPAddress>();
                list.AddRange(this.addresses);
                list.Add(address);
                addresses = list.ToArray();
                return true;
            }
            return false;
        }
        internal bool HasAddress(IPAddress address)
        {
            foreach (IPAddress a in addresses)
            {
                if (a.Equals(address))
                {
                    return true;
                }
            }
            return false;
        }

        public static string EscapeFullName(string fullName)
        {
            return fullName.Replace("\\", "\\\\").Replace(".", "\\.");
        }

        public static string SanitizeName(string name)
        {
            return name.Replace(".", " ");
        }

        public static string UnescapeFullName(string fullName)
        {
            StringBuilder sb = new StringBuilder();
            bool beginEscape = false;
            string num = "";
            foreach (char c in fullName.ToCharArray())
            {
                if (beginEscape)
                {
                    if (Char.IsDigit(c))
                    {
                        num = num + c.ToString();
                        if (num.Length == 3)
                        {
                            int n = 0;
                            if (int.TryParse(num, out n))
                            {
                                sb.Append(Convert.ToChar(n));
                            }
                            beginEscape = false;
                        }
                    } else
                    {
                        sb.Append(c);
                        beginEscape = false;
                    }
                } else
                {
                    if (c == '\\')
                    {
                        beginEscape = true;
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
            }
            return sb.ToString();
        }


        public string MachineName { get { return machineName; } }
        public string Name { get { return name; } }
        public IPAddress[] Addresses { get { return addresses; } }
        public int Port { get { return port; } }

        public override string ToString()
        {
            return ToString(machineName, name);
        }

        public static string ToString(string machineName, string name)
        {
            return machineName + " (" + name + ")";
        }

        public static bool IsValid(string fullName)
        {
            if (!string.IsNullOrEmpty(fullName))
            {
                if (fullName.Contains("("))
                {
                    if (fullName.Contains(")"))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public static OMTAddress Create(string fullName, int port)
        {
            if (!IsValid(fullName)) return null;
            int index = fullName.IndexOf('(');
            string machineName = fullName.Substring(0, index).Trim();
            if (index > 0)
            {
               string name = fullName.Substring(index + 1);
               name = name.Substring(0, name.Length - 1);
               return new OMTAddress(machineName, name, port);
            }
            return null;
        }

        public static string GetMachineName(string fullName)
        {
            string[] s = fullName.Split('(');
            return s[0].Trim();
        }
        public static string GetName(string fullName)
        {
            int index = fullName.IndexOf('(');
            if (index > 0)
            {
                string name = fullName.Substring(index + 1);
                name = name.Substring(0, name.Length - 1);
                return name;
            }
            return "";
        }

    }

    internal class OMTAddressSorter : IComparer<OMTAddress>
    {
        public int Compare(OMTAddress x, OMTAddress y)
        {
            if (x != null && y != null)
            {
                return String.Compare(x.ToString(), y.ToString());
            }
            return 0;
        }
    }
}
