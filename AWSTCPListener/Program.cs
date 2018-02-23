using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Timers;

namespace AWSTCPListener
{
    static class Program
    {
        static TcpListener server = null;
        static StreamWriter sw = null;
        static TcpClient client = null;
        static string dataFilePath = "", logFilePath = "";
        static NetworkStream stream = null;
        static DateTime connStartTime, serverStartTime;
        static int byteCount = 0;
        static double transmitDuration = 0;
        static bool clientDisconnectedProperly = false;
        static Log log = null;
        static bool debugEnabled = true;

        public static void Main()
        {
            dataFilePath = "/home/administrator/awslistener/gen3data.dat";
            logFilePath = "/home/administrator/awslistener/gen3log.log";


            // Buffer for reading data
            byte[] bytes = new byte[1200];               //client is sending chunks of 1024 bytes
            string data = "";

            while (true)
            {
                REPEAT:
                try
                {
                    if (log != null && clientDisconnectedProperly == false)
                    {
                        log.Add(DateTime.Now, " The network read operation did not close in the expected manner after receiving " + byteCount + " bytes. "
                            + " The receive operation lasted " + Math.Round(DateTime.Now.Subtract(connStartTime).TotalSeconds, 1) + " seconds");
                        log.Save();
                    }
                    if (log != null && !log.IsClosed) log.Close();
                    log = new Log(logFilePath);    

                    server = new TcpListener(IPAddress.Any, 10005);
                    server.Start();
                    serverStartTime = DateTime.Now;
                    log.Add(DateTime.Now, "A new server session started successfully.");

                    clientDisconnectedProperly = false;
                    byteCount = 0;

                    if (debugEnabled)
                    {
                        Console.WriteLine("WIMEA AWS TCP Listener build 21-02-2018.");
                        Console.WriteLine("Server Started: OS Version: " + Environment.OSVersion.ToString() + "   " + serverStartTime.ToString());
                        Console.Write("Waiting for a connection:\t");
                    }

                    client = server.AcceptTcpClient();
                    sw = new StreamWriter(dataFilePath, true);

                    if (debugEnabled) Console.WriteLine("A client device has connected.");
                    connStartTime = DateTime.Now;
                    TimeSpan interval = connStartTime.Subtract(serverStartTime);

                    stream = client.GetStream();
                    log.Add(DateTime.Now, "A client connected after " + Math.Round(interval.TotalMinutes, 1) + " minutes");
                    // Get a stream object for reading and writing
                    if (debugEnabled) Console.WriteLine("\n Network Stream Established. \t LOCAL SERVER TIME: " + connStartTime.ToString() + "\n");
                    log.Add(DateTime.Now, "A Network stream was established with the client");
                    int i;
                    stream.ReadTimeout = 60000;
                    while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        data = Encoding.UTF8.GetString(bytes, 0, i);
                        byteCount += data.Length;

                        if (debugEnabled) Console.Write(data);

                        if (data.Contains("DISCON"))
                        {
                            clientDisconnectedProperly = true;
                            client.Close();
                            server.Stop();
                            sw.Close();
                            transmitDuration = DateTime.Now.Subtract(connStartTime).TotalSeconds;

                            if (debugEnabled) Console.WriteLine("\n\n Transmission lasted: " + transmitDuration);

                            log.Add(DateTime.Now, "The DISCONNECT command was received after " + byteCount + " bytes. Transmission lasted " + Math.Round(transmitDuration, 2) + " seconds");
                            log.Save();
                            goto REPEAT;
                        }
                        sw.Write(data);
                        sw.Flush();
                    }
                }
                catch (Exception ex)
                {
                    if (log != null)
                    {
                        log.Add(DateTime.Now, "ERROR: " + ex.Message +"--"+ ex.InnerException.Message);
                    }
                    if (sw != null) sw.Close();
                    if (client != null) client.Close();
                    if (server != null) server.Stop();
                    goto REPEAT;
                }
            }
        }
    }
}
    