//
// Copyright (c) 2018 The nanoFramework project contributors
// See LICENSE file in the project root for full license information.
//

using nanoFramework.Networking;

using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

using Windows.Devices.Gpio;

namespace HttpSamples.HttpListenerSample
{
    public class Program
    {
        public static void Main()
        {
            var networkHerlpers = new NetworkHelpers();
            networkHerlpers.SetupAndConnectNetwork(true);

            Console.WriteLine("Waiting for network up and IP address...");
            NetworkHelpers.IpAddressAvailable.WaitOne();

            Console.WriteLine("Waiting for valid Date & Time...");
            NetworkHelpers.DateTimeAvailable.WaitOne();

            Thread.Sleep(2000);

            string prefix = "http";
            int port = 80;

            Console.WriteLine("* Creating Http Listener: " + prefix + " on port " + port);
            HttpListener listener = new HttpListener(prefix, port);
            // Start Listener 
            listener.Start();
            // After Starting, several exceptions are thrown

            string ips = $"{NetworkInterface.GetAllNetworkInterfaces()[0].IPv4Address}, {NetworkInterface.GetAllNetworkInterfaces()[1].IPv4Address}, {NetworkInterface.GetAllNetworkInterfaces()[2].IPv4Address}";
            Console.WriteLine($"Listening for HTTP requests @ {ips}:{port} ...");

            while (true)
            {
                try
                {
                    // now wait on context for a connection
                    HttpListenerContext context = listener.GetContext();
                    if (context != null)
                    {
                        string url = context.Request.Url.OriginalString;
                        string responseString = $"<HTML><BODY>Hello world! This is nanoFramework.<br/>{DateTime.UtcNow.ToString()}<br/>{url}<br/>{ips}</BODY></HTML>";
                        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);

                        // Get the response stream and write the response content to it
                        context.Response.ContentLength64 = buffer.Length;
                        context.Response.OutputStream.Write(buffer, 0, buffer.Length);

                        // output stream must be closed
                        context.Response.Close();
                        // context must be closed
                        context.Close();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("* Error getting context: " + ex.Message + "\r\nSack = " + ex.StackTrace);
                }
            }
        }


    }
}
