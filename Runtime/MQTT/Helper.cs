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