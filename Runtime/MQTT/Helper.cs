using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

namespace Labsterium
{
    public class Helper
    {
        public static string GetIP()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());

            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "0.0.0.0";
        }
        public static async Task<int> GetRSSIAsync()
        {
            return await Task.Run(() =>
            {

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
                var process = new System.Diagnostics.Process
                {
                    StartInfo =
                    {
                    FileName = "netsh.exe",
                    Arguments = "wlan show interfaces",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                    }
                };
                process.Start();
                process.WaitForExit();
                var output = process.StandardOutput.ReadToEnd();
                if (output.Length > 18)
                {
                    var ns = output.IndexOf('%');
                    var str = output.Substring(ns - 3, 3);
                    str = str.Trim();
                    str = str.Trim('%');
                    int percent = int.Parse(str);
                    int maxRssi = -30, minRssi = -90;

                    int rssi = minRssi + Mathf.RoundToInt(percent * 0.01f * (maxRssi - minRssi));
                    return -rssi;
                }
#elif UNITY_STANDALONE_LINUX
                return 0;//TODO : RSSI Linux
#endif
                return 0;


            });
        }
    }
}