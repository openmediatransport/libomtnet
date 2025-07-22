using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace libomtnet
{
    public class OMTDiscovery : OMTBase
    {
        private string[] addresses = { };
        private OMTAddressSorter addressSorter = new OMTAddressSorter();
        internal List<OMTDiscoveryEntry> entries = new List<OMTDiscoveryEntry>();
        private List<IOMTDiscoveryNotify> notifications = new List<IOMTDiscoveryNotify>();
        protected object lockSync = new object();
        private static object sharedLockSync = new object();
        private static OMTDiscovery instance = null;
        private DateTime lastCleared;

        protected OMTDiscovery()
        {
            entries = new List<OMTDiscoveryEntry>();
        }

        ///<summary>
        ///Get the shared instance of OMTDiscovery used by all Senders and Receivers with a process.
        ///This should never be disposed, or only disposed only when last sender or receiver has been disposed and no further use of this library is expected.
        /// </summary>
        public static OMTDiscovery GetInstance()
        {
            lock (sharedLockSync)
            {
                if (instance == null)
                {
                    switch (OMTPlatform.GetPlatformType())
                    {
                        case OMTPlatformType.Win32:
                            instance = new win32.OMTDiscoveryWin32();
                            break;
                        case OMTPlatformType.MacOS:
                        case OMTPlatformType.iOS:
                            instance = new mac.OMTDiscoveryMac();
                            break;
                        case OMTPlatformType.Linux:
                            instance = new linux.OMTDiscoveryAvahi();
                            break;
                        default:
                            instance = new OMTDiscovery();
                            break;
                    }
                }
            }
            return instance;
        }

        internal void Subscribe(IOMTDiscoveryNotify notify)
        {
            lock (lockSync)
            {
                if (notifications.Contains(notify) == false)
                {
                    notifications.Add(notify);
                }
            }
        }

        internal void Unsubscribe(IOMTDiscoveryNotify notify)
        {
            lock (lockSync)
            {
                if (notifications.Contains(notify))
                {
                    notifications.Remove(notify);
                }
            }
        }
        internal void OnNewAddress(OMTAddress address)
        {
            if (address.Addresses.Length > 0)
            {
                IOMTDiscoveryNotify[] n = null;
                lock (lockSync)
                {
                    n = notifications.ToArray();
                }
                if (n != null)
                {
                    foreach (IOMTDiscoveryNotify notify in n)
                    {
                        notify.Notify(address);
                    }
                }
            }
        }

        internal OMTDiscoveryEntry GetEntry(OMTAddress address)
        {
            lock (lockSync)
            {
                foreach (OMTDiscoveryEntry entry in entries)
                {
                    if (entry.Address.ToString() == address.ToString())
                    {
                        return entry;
                    }
                }                
            }
            return null;
        }

        internal OMTDiscoveryEntry GetEntry(string fullName)
        {
            lock (lockSync)
            {
                foreach (OMTDiscoveryEntry entry in entries)
                {
                    if (entry.Address.ToString() == fullName)
                    {
                        return entry;
                    }
                }
            }
            return null;
        }

        internal bool RemoveDiscoveredEntry(string fullName)
        {
            lock (lockSync)
            {
                OMTDiscoveryEntry entry = GetEntry(fullName);
                if (entry != null)
                {
                    if (entry.Status == OMTDiscoveryEntryStatus.Discovered)
                    {
                        RemoveEntry(entry.Address, true);
                        OMTLogging.Write("Remove: " + entry.Address.ToString() + ":" + entry.Address.Port, "OMTDiscovery");
                        return true;
                    }
                }
            }
            return false;
        }

        internal OMTDiscoveryEntry UpdateDiscoveredEntry(string fullName, int port, IPAddress[] addresses)
        {
            lock (lockSync)
            {
                OMTDiscoveryEntry entry = GetEntry(fullName);
                if (entry == null)
                {
                    OMTAddress address = OMTAddress.Create(fullName, port);
                    entry = new OMTDiscoveryEntry(address);
                    entry.ChangeStatus(OMTDiscoveryEntryStatus.Discovered);
                    if (AddEntry(entry))
                    {
                        OMTLogging.Write("New: " + fullName + ":" + port, "OMTDiscovery");
                        if (addresses != null)
                        {
                            foreach (IPAddress ip in addresses)
                            {
                                address.AddAddress(ip);
                                OMTLogging.Write("NewIP: " + fullName + ":" + port + "," + ip.ToString(),"OMTDiscovery");
                            }
                        }
                        return entry;
                    }
                } else
                {
                    if (addresses != null)
                    {
                        bool newIp = false;
                        foreach (IPAddress ip in addresses)
                        {
                            if (entry.Address.AddAddress(ip))
                            {
                                OMTLogging.Write("AddIP: " + fullName + ":" + port + "," + ip.ToString(), "OMTDiscovery");
                                newIp = true;
                            }
                        }
                        if (newIp)
                        {
                            OnNewAddress(entry.Address);
                        }
                    }
                    return entry;
                }
            }
            return null;
        }

        internal bool AddEntry(OMTDiscoveryEntry entry)
        {
            lock (lockSync)
            {
                if (entries.Contains(entry) == false)
                {
                    entries.Add(entry);
                    RefreshAddresses();
                    return true;
                }
            }
            return false;
        }

        private void RefreshAddresses ()
        {
            List<string> addresses = new List<string>();
            foreach (OMTDiscoveryEntry entry in entries)
            {
                addresses.Add(entry.Address.ToString());
            }
            this.addresses = addresses.ToArray();
        }

        internal bool RemoveEntry(OMTAddress address, bool dispose)
        {
            lock (lockSync)
            {
                foreach (OMTDiscoveryEntry entry in entries)
                {
                    if (entry.Address.ToString() == address.ToString())
                    {
                        entries.Remove(entry);
                        if (dispose)
                        {
                            entry.Dispose();
                        }
                        RefreshAddresses();
                        return true;
                    }
                }
            }
            return false;
        }

        internal virtual bool DeregisterAddress(OMTAddress address)
        {
            return RemoveEntry(address, true);
        }
        internal virtual bool RegisterAddress(OMTAddress address)
        {
            OMTDiscoveryEntry entry = GetEntry(address);
            if (entry == null)
            {
                entry = new OMTDiscoveryEntry(address);
                AddEntry(entry);
                return true;
            }
            return false;
        }
        internal OMTAddress FindByFullNameOrUrl(string address)
        {
            if (string.IsNullOrEmpty(address)) { return null; }
            if (address.ToLower().StartsWith(OMTConstants.URL_PREFIX))
            {
                Uri u = null;
                if (Uri.TryCreate(address, UriKind.Absolute, out u))
                {
                    if (u.Port > 0)
                    {
                        OMTAddress a = new OMTAddress(u.Host, u.Port.ToString(), u.Port);
                        IPAddress[] ips = OMTUtils.ResolveHostname(u.Host);
                        if (ips != null && ips.Length > 0)
                        {
                            foreach (IPAddress ip in ips)
                            {
                                a.AddAddress(ip);
                            }
                            return a;
                        }
                    }
                }
            }
            else
            {
                return FindByFullName(address);
            }
            return null;
        }
        internal OMTAddress FindByFullName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) { return null; }
            lock (lockSync)
            {
                foreach (OMTDiscoveryEntry  entry in entries)
                {
                    if (entry.Address.ToString().Equals(OMTAddress.SanitizeName(fullName)))
                    {
                        return entry.Address;
                    }
                }
            }
            return null;
        }

        internal void RemoveExpiredAddresses()
        {
            lock (lockSync)
            {
                lastCleared = DateTime.Now;

                for (int i = entries.Count - 1; i >= 0; i--)
                {
                    OMTDiscoveryEntry entry = entries[i];
                    if (entry.Status == OMTDiscoveryEntryStatus.Discovered)
                    {
                        if (entry.Expiry > DateTime.MinValue && entry.Expiry < DateTime.Now)
                        {
                            entries.RemoveAt(i);
                            OMTLogging.Write("ExpiredAddress: " + entry.Address.ToString(), "OMTDiscoveryWin32");
                        }
                    }
                }
            }
        }

        protected int GetRegisteredEntryCount()
        {
            int count = 0;
            lock (lockSync)
            {
                foreach (OMTDiscoveryEntry entry in entries)
                {
                    if (entry.Status == OMTDiscoveryEntryStatus.Registered)
                    {
                        count++;
                    }
                }
            }
            return 0;
        }

        internal OMTDiscoveryEntry GetEntryByPort(int port, bool discovered)
        {
            lock (lockSync)
            {
                foreach (OMTDiscoveryEntry entry in entries)
                {
                    if (entry.Address.Port == port)
                    {
                        if (discovered)
                        {
                            if (entry.Status == OMTDiscoveryEntryStatus.Discovered)
                            {
                                return entry;
                            }
                        }
                        else
                        {
                            if (entry.Status != OMTDiscoveryEntryStatus.Discovered)
                            {
                                return entry;
                            }
                        }
                    }
                }
            }
            return null;
        }

        ///<summary>
        /// Retrieve a list of Sources currently available on the network
        /// </summary>
        public string[] GetAddresses()
        {
            if (lastCleared < DateTime.Now.AddSeconds(-10))
            {
                RemoveExpiredAddresses();
            }
            return addresses;
        }


        private void DisposeEntries()
        {
            OMTDiscoveryEntry[] e = null;
            lock (lockSync)
            {
                e = entries.ToArray();
            }
            if (e != null)
            {
                foreach (OMTDiscoveryEntry rr in e)
                {
                    if (rr != null)
                    {
                        rr.Dispose(); //The cancel request calls OnComplete, so run outside of lock here
                    }
                }
            }
            lock (lockSync)
            {
                entries.Clear();
            }
        }

        protected override void DisposeInternal()
        {
            DisposeEntries();
            base.DisposeInternal();
        }

    }

    internal interface IOMTDiscoveryNotify
    {
        void Notify(OMTAddress address);
    }

    internal enum OMTDiscoveryEntryStatus
    {
        None = 0,
        PendingRegister,
        PendingDeRegister,
        PendingRegisterAfterDeRegister,
        PendingDeRegisterAfterRegister,
        Registered,
        Discovered
    }
    internal class OMTDiscoveryEntry : OMTBase
    {
        private OMTAddress address;
        private OMTDiscoveryEntryStatus status;
        private GCHandle handle;
        private DateTime expiry;

        public OMTAddress Address { get { return address; } }
        public OMTDiscoveryEntryStatus Status { get { return status; } }

        public OMTDiscoveryEntry(OMTAddress address)
        {
            this.address = address;
            this.status = OMTDiscoveryEntryStatus.None;
        }

        public DateTime Expiry
        {
            get { return expiry; }
            set { expiry = value; }
        }

        public void ChangeStatus(OMTDiscoveryEntryStatus status)
        {
            this.status = status;
        }
        public static OMTDiscoveryEntry FromIntPtr(IntPtr p)
        {
            GCHandle h = GCHandle.FromIntPtr(p);
            if (h.IsAllocated)
            {
                OMTDiscoveryEntry q = (OMTDiscoveryEntry)h.Target;
                return q;
            }
            return null;
        }

        public IntPtr ToIntPtr()
        {
            if (!handle.IsAllocated)
            {
                handle = GCHandle.Alloc(this);
            }
            return GCHandle.ToIntPtr(handle);
        }

        protected override void DisposeInternal()
        {
            if (handle.IsAllocated)
            {
                handle.Free();
            }
            base.DisposeInternal();
        }

    }

}
