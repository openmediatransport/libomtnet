using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Xml;

namespace libomtnet
{
    /// <summary>
    /// This is an enternal class used to manage connection to the OMT Discovery Server
    /// This should not be used directly by client apps, and is declared public for internal testing purposes only.
    /// </summary>
    public class OMTDiscoveryClient : OMTBase
    {
        private OMTReceive client = null;
        private OMTDiscovery discovery = null;
        private Thread processingThread = null;
        private bool threadExit = false;

        public OMTDiscoveryClient(string address, OMTDiscovery discovery)
        {
            this.discovery = discovery;
            this.client = new OMTReceive(address, this);
            StartClient();
            OMTLogging.Write("Started: " + address, "OMTDiscoveryClient");
        }

        private void StartClient()
        {
            if (processingThread == null)
            {
                threadExit = false;
                processingThread = new Thread(ProcessThread);
                processingThread.IsBackground = true;
                processingThread.Start();
            }
        }
        private void StopClient()
        {
            if (processingThread != null)
            {
                threadExit = true;
                processingThread.Join();
                processingThread = null;
            }
        }

        internal void SendAddress(OMTAddress address)
        {
            try
            {
                string xml = address.ToXML();
                int bytes = client.SendMetadata(new OMTMetadata(0, xml));
            }
            catch (Exception ex)
            {
                OMTLogging.Write(ex.ToString(), "OMTDiscoveryClient");
            }
        }

        internal void SendAll()
        {
            try
            {
                OMTAddress[] addresses = discovery.GetAddressesInternal();
                if (addresses != null)
                {
                    foreach (OMTAddress a in addresses)
                    {
                        SendAddress(a);
                    }
                }
            }
            catch (Exception ex)
            {
                OMTLogging.Write(ex.ToString(), "OMTDiscoveryClient");
            }
        }

        internal void Connected()
        {
            try
            {
                SendAll();
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
                while (threadExit == false)
                {
                    if (client.Receive(100, ref frame))
                    {
                        if (frame != null)
                        {
                            try
                            {
                                OMTAddress a = OMTAddress.FromXML(frame.XML);
                                if (a != null)
                                {
                                    if (a.removed)
                                    {
                                        OMTLogging.Write("RemovedFromServer: " + a.ToString(), "OMTDiscoveryClient");
                                        discovery.RemoveEntry(a, true);
                                    }
                                    else
                                    {
                                        OMTLogging.Write("NewFromServer: " + a.ToString(), "OMTDiscoveryClient");
                                        discovery.UpdateDiscoveredEntry(a.ToString(), a.Port, a.Addresses);
                                    }
                                } else
                                {
                                    OMTLogging.Write("Invalid XML Received: " + frame.XML, "OMTDiscoveryClient");
                                }
                            }
                            catch (Exception ex)
                            {
                                OMTLogging.Write(ex.ToString(), "OMTDiscoveryClient");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OMTLogging.Write(ex.ToString(), "OMTDiscoveryClient");
            }
        }

        protected override void DisposeInternal()
        {
            StopClient();
            if (client != null)
            {
                client.Dispose();
                client = null;
            }
            discovery = null;
            base.DisposeInternal();
        }
    }
}
