using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

namespace Labsterium
{
    [System.Serializable]
    public struct MecaInfo
    {
        public NetworkInfo Network;
        public MQTTInfo MQTT;
        public HardwareInfo Hardware_Infos;
    }
    [System.Serializable]
    public struct NetworkInfo
    {
        public string IP;
        public string SSID;
        public int Channel;
        public int RSSI;
    }
    [System.Serializable]
    public struct MQTTInfo
    {
        public string clientid;
    }
    [System.Serializable]
    public struct HardwareInfo
    {
        public string Type;
    }
    public class Helper
    {


        static async Task<string> Command(string fn, string args)
        {
            return await Task<string>.Run(() =>
            {
                try
                {
                    var process = new System.Diagnostics.Process
                    {
                        StartInfo =
                                {
                                    FileName = fn,
                                    Arguments = args,
                                    UseShellExecute = false,
                                    RedirectStandardOutput = true,
                                    RedirectStandardError = true,
                                    CreateNoWindow = true,
                                    // StandardOutputEncoding = Encoding.GetEncoding(loc)
                                }
                    };
                    process.Start();
                    process.WaitForExit();
                    if (process.StandardError.ReadToEnd() != "")
                        return process.StandardError.ReadToEnd();
                    return process.StandardOutput.ReadToEnd();
                }
                catch (System.Exception e)
                {
                    throw;
                }
            });

        }

        public static async Task<NetworkInfo> GetNetworkInfo()
        {
            var ni = new NetworkInfo();
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            //TODO
            string resultWin = await Helper.Command("netsh", "wlan show interfaces");
            var lines = resultWin.Split('\n');
            foreach (var line in lines)
            {
                var l = line.TrimStart();

                if (l.Contains("Signal"))
                {
                    var sig = int.Parse(l.Split(':')[1].Split('%')[0].Trim());
                    ni.RSSI = Mathf.RoundToInt(90 - sig * .6f);
                }
                if (l.Contains("SSID") && !l.Contains("BSSID"))
                    ni.SSID = l.Split(':')[1];
                if (l.Contains("Canal"))
                {
                    var chan = int.Parse(l.Split(':')[1].Trim());
                    ni.Channel = chan;
                }

            }
            // Debug.Log("resultWin" + resultWin);
            resultWin = await Helper.Command("ipconfig", "");
            var idx = resultWin.IndexOf("IPv4");
            if (idx > 0)
            {
                var idx1 = resultWin.IndexOf(':', idx) + 1;
                var idx2 = resultWin.IndexOf('\n', idx);
                var substrIP = resultWin[idx1..idx2].Trim();
                Debug.Log("substrIP" + substrIP);
                ni.IP = substrIP;
            }
            Debug.Log("resultWin" + resultWin);
            return ni;
#elif UNITY_STANDALONE_LINUX
try
{
        string resultDeb = await Helper.Command("/sbin/iw", "wlp2s0 link");
        int first = resultDeb.IndexOf("signal:") + "signal: ".Length;
        int last = resultDeb.IndexOf(" ", first);
        ni.RSSI = int.Parse(resultDeb[first..last]);
        resultDeb = await Helper.Command("/sbin/iw", "wlp2s0 info");
        first = resultDeb.IndexOf("channel") + "channel ".Length;
        last = resultDeb.IndexOf(" ", first);
        ni.Channel = int.Parse(resultDeb[first..last]);
        first = resultDeb.IndexOf("ssid") + "ssid ".Length;
        last = resultDeb.IndexOf("\n", first);
        ni.SSID = resultDeb[first..last];
        resultDeb = await Helper.Command("/bin/ip", "addr show wlp2s0");
        first = resultDeb.IndexOf("inet ") + "inet ".Length;
        last = resultDeb.IndexOf("/", first);
        ni.IP = resultDeb[first..last];
        return ni;
    
}
catch (System.Exception e)
{
    Debug.Log("Error" + e);
    return ni;
}

#endif
        }
        public static void Quit()
        {
            Application.Quit(0);
        }
        public static void Reboot()
        {
            Application.Quit(-1);
        }
    }
}