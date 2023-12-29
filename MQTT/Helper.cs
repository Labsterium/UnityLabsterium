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
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            return await Task.Run(() =>
            {
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
#elif UNITY_ANDROID
        // AndroidJNI.AttachCurrentThread();
        AndroidJavaObject mWiFiManager = null;
        if (mWiFiManager == null)
        {
            using (AndroidJavaObject activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity"))
            {
                mWiFiManager = activity.Call<AndroidJavaObject>("getSystemService", "wifi");
            }
        }
        int rssi = mWiFiManager.Call<AndroidJavaObject>("getConnectionInfo").Call<int>("getRssi");
        // AndroidJNI.DetachCurrentThread();
        return -rssi;

#endif
                return 0;
            });
        }
        public static int GetRSSI()
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

            var output = process.StandardOutput.ReadToEnd().Split('\n');
            if (output.Length > 18)
            {
                var str = output[18].Split(":")[1];
                str = str.Trim();
                str = str.Trim('%');
                int percent = int.Parse(str);
                int maxRssi = -30, minRssi = -90;

                int rssi = minRssi + Mathf.RoundToInt(percent * 0.01f * (maxRssi - minRssi));
                return -rssi;
            }
#elif UNITY_ANDROID
        // AndroidJNI.AttachCurrentThread();
        AndroidJavaObject mWiFiManager = null;
        if (mWiFiManager == null)
        {
            using (AndroidJavaObject activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity"))
            {
                mWiFiManager = activity.Call<AndroidJavaObject>("getSystemService", "wifi");
            }
        }
        int rssi = mWiFiManager.Call<AndroidJavaObject>("getConnectionInfo").Call<int>("getRssi");
        // AndroidJNI.DetachCurrentThread();
        return -rssi;

#endif
            return 0;
        }

        public static void WakeAndroid()
        {
            AndroidJNI.AttachCurrentThread();
            AndroidJavaObject pm = null;
            using (AndroidJavaObject activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity"))
            {
                // activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                // {

                // alarmManager = activity.Call<AndroidJavaObject>("getSystemService", "alarm");
                pm = activity.Call<AndroidJavaObject>("getSystemService", "power");
                AndroidJavaObject wakelock = pm.Call<AndroidJavaObject>("newWakeLock", new object[] { 0x1000000a, "Labsterium" });
                wakelock.Call("acquire", new object[] { 1000 });
                wakelock.Call("release");

                // using (AndroidJavaObject wakeClass = new AndroidJavaObject("com.labsterium.wakeup.MyClass"))
                // {
                //     wakeClass.Call("wake");
                // }
                // }));
            }
            AndroidJNI.DetachCurrentThread();
        }

        internal static void Vibrate()
        {
            using (AndroidJavaObject activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity"))
            {
                AndroidJavaObject vb = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                AndroidJavaClass ve = new AndroidJavaClass("android.os.VibrationEffect");
                // AndroidJavaObject vibeffect = ve.CallStatic<AndroidJavaObject>("createPredefined", 5);
                AndroidJavaObject vibeffect = ve.CallStatic<AndroidJavaObject>("createOneShot", new object[] { (long)100, (int)255 });
                vb.Call("vibrate", vibeffect);
            }

        }
    }
}