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

        private static NetworkInterface m_NetworkInterfaces_Wifi = null;
        private static NetworkInterface m_NetworkInterfaces_Lan = null;


        internal void SetupAndConnectNetwork(bool requiresDateTime = true)
        {
            Sntp.Server1 = "0.de.pool.ntp.org";
            Sntp.Server2 = "1.de.pool.ntp.org";
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
                    if (n.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                    //if (n.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                    {
                        Console.WriteLine("Network connection is: Ethernet");

                        n.EnableAutomaticDns();
                        n.EnableDhcp();
                        //n.EnableStaticIPv4("10.10.10.131", "255.255.255.0", "10.10.10.1");

                        m_NetworkInterfaces_Lan = n;
                    }

                    if (n.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                    {
                        Console.WriteLine("Network connection is: Wi-Fi");
                        Wireless80211Configuration wc = Wireless80211Configuration.GetAllWireless80211Configurations()[n.SpecificConfigId];
                        Console.WriteLine($"Ssid:{wc.Ssid},Encryption:{wc.Encryption},Password:{wc.Password},Authentication:{wc.Authentication}");

                        wc.Authentication = AuthenticationType.WPA2;
                        wc.Encryption = EncryptionType.WPA2_PSK;
                        wc.Ssid = c_SSID;
                        wc.Password = c_AP_PASSWORD;

                        //wc.Ssid = "";
                        //wc.Password = "";

                        wc.SaveConfiguration();

                        //n.EnableStaticIPv4("192.168.0.250", "255.255.255.0", "192.168.0.1");
                        n.EnableAutomaticDns();
                        n.EnableDhcp();

                        m_NetworkInterfaces_Wifi = n;
                    }
                }

                if (m_NetworkInterfaces_Lan == null && m_NetworkInterfaces_Wifi == null) throw new NotSupportedException("ERROR: there is no network interface configured.\r\nOpen the 'Edit Network Configuration' in Device Explorer and configure one.");

                // check if we have an IP
                CheckIP();

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


        private static void CheckIP()
        {
            Console.WriteLine($"Checking for IP ");
            bool found = false;
            found = found | CheckIpByInterface(m_NetworkInterfaces_Lan);
            found = found | CheckIpByInterface(m_NetworkInterfaces_Wifi);

            Thread.Sleep(2000);
            if (found) IpAddressAvailable.Set();
        }

        private static bool CheckIpByInterface(NetworkInterface ni)
        {
            if (ni != null && ni.IPv4Address != null && ni.IPv4Address.Length > 0)
            {
                if (ni.IPv4Address[0] != '0')
                {
                    Console.WriteLine($"We have an IP: {ni.IPv4Address}");
                    return true;
                }
            }
            return false;
        }

        private static void AddressChangedCallback(object sender, EventArgs e)
        {
            CheckIP();
        }
    }
}
