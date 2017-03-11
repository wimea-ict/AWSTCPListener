using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Timers;

class MyTcpListener
{
    static TcpListener server = null;
    static bool exception = false;
    static StreamWriter sw = null;
    static TcpClient client = null;
    static string filepath = "";


    public static void Main()
    {
        if (Environment.OSVersion.ToString().Contains("Windows"))
        {
            filepath = "data.txt";
        }
        else               //Unix
        {
            filepath = "/home/user-here/folder-path-here/data.txt";  //EDIT THIS LINE
        }
        
    REPEAT:
        try
        {
            server = new TcpListener(IPAddress.Any, 10000);
            // Start listening for client requests.
            server.Start(2);
            Console.WriteLine("Server Started: OS Version: " + Environment.OSVersion.ToString());
            // Buffer for reading data
            byte[] bytes = new byte[4096];
            string data = null;

            // Enter the listening loop. 
            while (true)
            {
                Console.Write("Waiting for a connection:\t");
                client = server.AcceptTcpClient();
                sw = new StreamWriter(filepath, true);
                Console.WriteLine("A client device has connected.");

                // Get a stream object for reading and writing
                NetworkStream stream = client.GetStream();
                Console.WriteLine("Network Stream Established. \t LOCAL SERVER TIME: " + DateTime.Now.ToString() + "\n\n");
                stream.WriteTimeout = 60000;
                int i;

                // Loop to receive all the data sent by the client. 
                while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    // Translate data bytes to a ASCII string.
                    data = Encoding.UTF8.GetString(bytes, 0, i);
                    Console.Write(data);
                    sw.Write(data);
                    sw.Flush();
                }             
            }
        }
        catch (Exception e)
        {
            //something went wrong
            exception = true;
            sw.Close();  
            server.Stop();
            client.Close();
            Console.WriteLine("Exception: {0}", e);        
        }

        if (exception)
        {
            goto REPEAT;
        }
    }
}
