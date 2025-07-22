using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Threading;
using System.ComponentModel;

namespace libomtnet
{
    public class OMTLogging
    {
        private static FileStream logStream;
        private static StreamWriter logWriter;
        private static object lockSync = new object();
        private static Thread loggingThread;
        private static bool threadRunning;
        private static Queue<string> queue = new Queue<string>();
        private static AutoResetEvent readyEvent = new AutoResetEvent(false);

        static OMTLogging()
        {
            string name = GetProcessNameAndId();
            if (name != null)
            {
                string szPath = OMTPlatform.GetInstance().GetStoragePath();
                if (Directory.Exists(szPath) == false) { Directory.CreateDirectory(szPath); }
                SetFilename(szPath + Path.DirectorySeparatorChar + name + ".log");
            }
            loggingThread = new Thread(ProcessLog);
            loggingThread.IsBackground = true;
            threadRunning = true;
            loggingThread.Start();
        }

        private static string GetProcessNameAndId()
        {
            Process process = Process.GetCurrentProcess();
            if (process != null)
            {
                ProcessModule module = process.MainModule;
                if (module != null) //Some platforms, notably iOS return null
                {
                    return module.ModuleName + process.Id;
                } else
                {
                    return process.Id.ToString();
                }
            }
            return null;
        }

        private static void ProcessLog()
        {
            try
            {
                while (threadRunning)
                {
                    readyEvent.WaitOne();
                    lock (lockSync)
                    {
                        if (logWriter != null)
                        {
                            while (queue.Count > 0)
                            {
                                logWriter.WriteLine(queue.Dequeue());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString(), "OMTLogging.ProcessLog");
            }
        }

        public static void SetFilename(string filename)
        {
            lock (lockSync)
            {
                if (logStream != null)
                {
                    logStream.Close();
                }
                logStream = new FileStream(filename,FileMode.OpenOrCreate, FileAccess.Write);
                logStream.Position = logStream.Length;
                logWriter = new StreamWriter(logStream);
                logWriter.AutoFlush = true;
                Debug.WriteLine("OMTLogging.SetFilename: " + filename);
            }
        }
        public static void Write(string message, string source)
        {
            try
            {
                string line = DateTime.Now.ToString() + ",[" + source + "]," + message;
                Debug.WriteLine(line);
                lock (lockSync)
                {
                    if (logWriter != null)
                    {
                        queue.Enqueue(line);
                        readyEvent.Set();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
    }
}
