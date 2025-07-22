using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;

namespace libomtnet
{
    public class OMTSettings
    {
        private static object globalLock = new object();
        private object instanceLock = new object();
        private string filename;
        private XmlDocument document;
        private XmlNode rootNode;
        private static OMTSettings instance;
        public static OMTSettings GetInstance()
        {
            lock (globalLock)
            {
                if (instance == null)
                {
                    string sz = OMTPlatform.GetInstance().GetStoragePath() + Path.DirectorySeparatorChar + "settings.xml";
                    instance = new OMTSettings(sz);
                }
                return instance;
            }
        }
        public OMTSettings(string filename)
        {
            this.filename = filename;
            lock (globalLock)
            {
                document = new XmlDocument();
                try
                {
                    if (File.Exists(filename))
                    {
                        document.Load(filename);
                        rootNode = document.DocumentElement;
                    }
                }
                catch (Exception ex)
                {
                    OMTLogging.Write(ex.ToString(), "OMTSettings.New");
                }
                if (rootNode == null)
                {
                    rootNode = document.CreateElement("Settings");
                    document.AppendChild(rootNode);
                }
            }
        }
        public void Save()
        {
            lock (globalLock)
            {
                using (XmlTextWriter writer = new XmlTextWriter(filename, null))
                {
                    writer.Formatting = Formatting.Indented;
                    document.Save(writer);
                }
            }
        }
        public string GetString(string key, string defaultValue)
        {
            lock (instanceLock)
            {
                if (rootNode != null)
                {
                    XmlNode node = rootNode.SelectSingleNode(key);
                    if (node != null)
                    {
                        return node.InnerText;
                    }
                }
                return defaultValue;
            }
        }
        public void SetString(string key, string value)
        {
            lock (instanceLock)
            {
                if (rootNode != null)
                {
                    XmlNode node = rootNode.SelectSingleNode(key);
                    if (node == null)
                    {
                        node = document.CreateElement(key);
                        document.AppendChild(node);
                    }
                    node.InnerText = value;
                }
            }
        }

        public int GetInteger(string key, int defaultValue)
        {
            string value = GetString(key, null);
            if (!string.IsNullOrEmpty(value))
            {
                int v = 0;
                if (int.TryParse(value, out v))
                {
                    return v;
                }
            }
            return defaultValue;
        }
        public void SetInteger(string key, int value)
        {
            SetString(key, value.ToString());
        }
    }
}
