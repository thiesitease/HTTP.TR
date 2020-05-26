//
// Copyright (c) 2018 The nanoFramework project contributors
// See LICENSE file in the project root for full license information.
//

using nanoFramework.Runtime.Events;

using System;
using System.Net.NetworkInformation;
using System.Threading;

namespace nanoFramework.Networking
{
    public class NetworkHelpers
    {
        private const string c_SSID = "gemelo-tr";
        private const string c_AP_PASSWORD = "1234567890";

        private static bool _requiresDateTime;

        static public ManualResetEvent IpAddressAvailable = new ManualResetEvent(false);
        static public ManualResetEvent DateTimeAvailable = new ManualResetEvent(false);

        private static NetworkInterface m_NetworkInterface = null;


        internal void SetupAndConnectNetwork(bool requiresDateTime = false)
        {

            Console.WriteLine($"Sntp.Server1:{Sntp.Server1}");
            Console.WriteLine($"Sntp.Server2:{Sntp.Server2}");

            //Sntp.Server1 = "10.10.10.10";
            //Sntp.Server2 = "10.10.10.10";

            Console.WriteLine($"Sntp.Server1:{Sntp.Server1}");
            Console.WriteLine($"Sntp.Server2:{Sntp.Server2}");


            NetworkChange.NetworkAddressChanged += AddressChangedCallback;
            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;

            _requiresDateTime = requiresDateTime;
            new Thread(WorkingThread).Start();
        }

        private static void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            Console.WriteLine("Network availability changed");
        }

        internal static void WorkingThread()
        {
            NetworkInterface[] nis = NetworkInterface.GetAllNetworkInterfaces();

            if (nis.Length > 0)
            {
                // get the first interface

                foreach (var n in nis)
                {
                    if (n.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                    {
                        m_NetworkInterface = n;
                        break;
                    }
                }

                if (m_NetworkInterface == null) throw new NotSupportedException("ERROR: there is no network interface configured.\r\nOpen the 'Edit Network Configuration' in Device Explorer and configure one.");

                if (m_NetworkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                {
                    // network interface is Wi-Fi
                    Console.WriteLine("Network connection is: Wi-Fi");

                    Wireless80211Configuration wc = Wireless80211Configuration.GetAllWireless80211Configurations()[m_NetworkInterface.SpecificConfigId];

                    Console.WriteLine($"Ssid:{wc.Ssid},Encryption:{wc.Encryption},Password:{wc.Password},Authentication:{wc.Authentication}");

                    wc.Authentication = AuthenticationType.WPA2;
                    wc.Encryption = EncryptionType.WPA2_PSK;
                    wc.Ssid = c_SSID;
                    wc.Password = c_AP_PASSWORD;
                    wc.SaveConfiguration();
                    // Wi-Fi configuration matches
                    // (or can't be validated)
                }
                else
                {
                    // network interface is Ethernet
                    Console.WriteLine("Network connection is: Ethernet");
                }

                m_NetworkInterface.EnableAutomaticDns();
                m_NetworkInterface.EnableDhcp();

                // check if we have an IP
                CheckIPThread();

                if (_requiresDateTime)
                {
                    IpAddressAvailable.WaitOne();

                    SetDateTime();
                }
            }
            else
            {
                throw new NotSupportedException("ERROR: there is no network interface configured.\r\nOpen the 'Edit Network Configuration' in Device Explorer and configure one.");
            }
        }

        private static void SetDateTime()
        {


            int retryCount = 30;

            Console.WriteLine("Waiting for a valid date & time...");

            // if SNTP is available and enabled on target device this can be skipped because we should have a valid date & time
            while ((DateTime.UtcNow.Year < 2018) || (DateTime.UtcNow.Year > 2020))
            {

                // force update if we haven't a valid time after 30 seconds
                if (retryCount-- == 0)
                {
                    Console.WriteLine("Forcing SNTP update...");

                    Sntp.UpdateNow();

                    // reset counter
                    retryCount = 30;
                }

                // wait for valid date & time
                Thread.Sleep(1000);
            }

            Console.WriteLine($"We have valid date & time: {DateTime.UtcNow.ToString()}");

            DateTimeAvailable.Set();
        }

        private static void CheckIPThread()
        {
            Thread t = new Thread(CheckIP);
            t.Start();
        }

        private static void CheckIP()
        {
            bool proceed = true;
            int counter = 0;
            do
            {
                Console.WriteLine($"Checking for IP {counter}");

                NetworkInterface ni = m_NetworkInterface;
                if (ni.IPv4Address != null && ni.IPv4Address.Length > 0)
                {
                    if (ni.IPv4Address[0] != '0')
                    {
                        Console.WriteLine($"We have and IP: {ni.IPv4Address}");
                        IpAddressAvailable.Set();
                        proceed = false;
                    }
                }
                Thread.Sleep(2000);
                counter++;
            }
            while (proceed);
        }

        private static void AddressChangedCallback(object sender, EventArgs e)
        {
            CheckIP();
        }
    }
}
