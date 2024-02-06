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
    public class Helper
    {
        public static async Task<NetworkInfo> GetNetworkInfo()
        {
            var ni = new NetworkInfo();
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        //TODO
        return ni;
#elif UNITY_STANDALONE_LINUX
try
{
        string resultDeb = await Command("/sbin/iw", "wlp2s0 link");
        int first = resultDeb.IndexOf("signal:") + "signal: ".Length;
        int last = resultDeb.IndexOf(" ", first);
        ni.rssi = int.Parse(resultDeb[first..last]);
        resultDeb = await Command("/sbin/iw", "wlp2s0 info");
        first = resultDeb.IndexOf("channel") + "channel ".Length;
        last = resultDeb.IndexOf(" ", first);
        ni.channel = int.Parse(resultDeb[first..last]);
        first = resultDeb.IndexOf("ssid") + "ssid ".Length;
        last = resultDeb.IndexOf("\n", first);
        ni.ssid = resultDeb[first..last];
        resultDeb = await Command("/bin/ip", "addr show wlp2s0");
        first = resultDeb.IndexOf("inet ") + "inet ".Length;
        last = resultDeb.IndexOf("/", first);
        ni.ip = resultDeb[first..last];
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
    }
}