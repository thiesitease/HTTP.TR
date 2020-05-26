//
// Copyright (c) 2019 The nanoFramework project contributors
// See LICENSE file in the project root for full license information.
//

using nanoFramework.Networking;

using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace HttpSamples.HttpWebRequestSample
{
    public class ProgramRequest
    {
        public static void Main()
        {
            var networkHerlpers = new NetworkHelpers();
            networkHerlpers.SetupAndConnectNetwork(true);

            Console.WriteLine("Waiting for network up and IP address...");
            NetworkHelpers.IpAddressAvailable.WaitOne();

            Console.WriteLine("Waiting for valid Date & Time...");
            NetworkHelpers.DateTimeAvailable.WaitOne();

            //Test1();
            Test2();

            Thread.Sleep(Timeout.Infinite);
        }

        private static void Test2()
        {
            Uri uri = new Uri("http://files.gemelo.de/tr/Sayhi.txt");
            Console.WriteLine($"Uri: {uri.AbsoluteUri}");
            Console.WriteLine($"Port: {uri.Port}");
            Console.WriteLine($"Performing Http request to: {uri}");

            using (HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(uri))
            {
                httpWebRequest.Method = "GET";
                httpWebRequest.SslProtocols = System.Net.Security.SslProtocols.None;

                Console.WriteLine("Http response follows");
                Console.WriteLine(">>>>>>>>>>>>>");

                using (var response = httpWebRequest.GetResponse())
                {
                    using (var stream = response.GetResponseStream())
                    {
                        // read response in chunks of 1k

                        byte[] buffer = new byte[1024];
                        int bytesRead = 0;


                        do
                        {
                            bytesRead = stream.Read(buffer, 0, buffer.Length);
                            Console.Write(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                        }
                        while (bytesRead >= buffer.Length);
                    }
                }
                Console.WriteLine(">>>>>>>>>>>>>");
                Console.WriteLine("End of Http response");
            }

        }

    }
}
