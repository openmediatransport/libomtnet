using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Xml;

namespace libomtnet
{
    public class OMTDiscoveryServer : OMTBase
    {
        private OMTSend send;
        private Thread processingThread;
        private bool threadExit = false;

        private List<AddressEntry> addresses;

        private class AddressEntry
        {
            public OMTAddress Address;
            public IPEndPoint EndPoint;
        }

        public OMTDiscoveryServer(IPEndPoint endpoint)
        {
            addresses = new List<AddressEntry>();
            send = new OMTSend(endpoint, this);
        }

        public void StartServer()
        {
            if (processingThread == null)
            {
                threadExit = false;
                processingThread = new Thread(ProcessThread);
                processingThread.IsBackground = true;
                processingThread.Start();
            }
        }

        public void StopServer()
        {
            if (processingThread != null)
            {
                threadExit = true;
                processingThread.Join();
                processingThread = null;
            }
        }

        private AddressEntry GetEntry(string fullName, int port)
        {
            lock (addresses)
            {
                foreach (AddressEntry address in addresses)
                {
                    if (address.Address.ToString().Equals(fullName))
                    {
                        if (address.Address.Port == port)
                        {
                            return address;
                        }
                    }
                }
            }
            return null;
        }

        private void RemoveEntriesByEndPoint(IPEndPoint endpoint)
        {
            lock (addresses)
            {
                List<AddressEntry> toRemove = new List<AddressEntry>();
                string match = endpoint.ToString();
                foreach (AddressEntry address in addresses)
                {
                    if (address.EndPoint.ToString() == match)
                    {
                        toRemove.Add(address);
                    }
                }
                foreach (AddressEntry address in toRemove)
                {
                    RemoveEntry(address, endpoint);
                }
            }
        }

        private void AddEntry(OMTAddress address, IPEndPoint endpoint)
        {
            lock (addresses)
            {
                AddressEntry entry = new AddressEntry();
                entry.Address = address;
                entry.EndPoint = endpoint;
                addresses.Add(entry);
                SendEntry(entry, null);
                OMTLogging.Write("Added " + address.ToString() + " From " + endpoint.ToString(), "OMTDiscoveryServer");
                Console.WriteLine(endpoint.ToString() + " ADDED " + address.ToString());
            }
        }
        private void RemoveEntry(AddressEntry entry, IPEndPoint endpoint)
        {
            lock (addresses)
            {
                addresses.Remove(entry);
                SendEntry(entry, null);
                OMTLogging.Write("Removed " + entry.Address.ToString() + " From " + endpoint.ToString(), "OMTDiscoveryServer");
                Console.WriteLine(endpoint.ToString() + " REMOVED " + entry.Address.ToString());
            }
        }
        private void SendEntry(AddressEntry entry, IPEndPoint endpoint)
        {
            string xml = entry.Address.ToXML();
            send.SendMetadata(new OMTMetadata(0, xml), endpoint);
        }
        private void SendAllToEndpoint(IPEndPoint endpoint)
        {
            lock (addresses)
            {
                foreach (AddressEntry entry in addresses)
                {
                    SendEntry(entry, endpoint);
                }
            }
        }
        internal void Connected(IPEndPoint endpoint)
        {
            try
            {
                SendAllToEndpoint(endpoint);
                Console.WriteLine("Connected: " + endpoint.ToString());
            }
            catch (Exception ex)
            {
                OMTLogging.Write(ex.ToString(), "OMTDiscoveryServer");
            }
        }

        internal void Disconnected(IPEndPoint endpoint)
        {
            try
            {
                RemoveEntriesByEndPoint(endpoint);
                Console.WriteLine("Disconnected: " + endpoint.ToString());
            }
            catch (Exception ex)
            {
                OMTLogging.Write(ex.ToString(), "OMTDiscoveryServer");
            }
        }

        private void ProcessThread()
        {
            try
            {
                OMTMetadata frame = null;
                XmlDocument xml = new XmlDocument();
                while (threadExit == false)
                {
                    if (send.Receive(100, ref frame))
                    {
                        try
                        {
                            if (frame != null)
                            {
                                OMTAddress a = OMTAddress.FromXML(frame.XML);
                                if (a != null)
                                {
                                    AddressEntry entry = GetEntry(a.ToString(), a.Port);
                                    if (entry == null)
                                    {
                                        if (!a.removed)
                                        {
                                            a.ClearAddresses(); //Any IP addresses provided by client (typically loopback) are cleared so only detected IP is used.
                                            a.AddAddress(frame.Endpoint.Address);
                                            AddEntry(a, frame.Endpoint);
                                        }

                                    } else
                                    {
                                        if (a.removed)
                                        {
                                            RemoveEntry(entry, frame.Endpoint);
                                        }
                                    }                                  
                                    
                                } else
                                {
                                    OMTLogging.Write("Invalid XML Received: " + frame.XML, "OMTDiscoveryServer");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            OMTLogging.Write(ex.ToString(), "OMTDiscoveryServer");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OMTLogging.Write(ex.ToString(), "OMTDiscoveryServer");
            }
        }

        protected override void DisposeInternal()
        {
            StopServer();
            if (send != null)
            {
                send.Dispose();
                send = null;
            }
            base.DisposeInternal();
        }
    }
}
