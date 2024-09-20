using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using System.IO;
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using Labsterium;
public class VideoConverter
{
    TimeSpan maxDuration, convertedDuration;
    public void Connected()
    {
        MQTT_Labsterium.instance.SendMQTTMessageToTopic("FTP", MQTT_Labsterium.instance.mecaName);
    }
    public void ConvertVideo(List<string> args)
    {
        var path = Helper.GetFilePath();
        string nameVideo = args[0];
        if (File.Exists(path + nameVideo))
        {
            try
            {
                var nameFile = nameVideo[..^(nameVideo.LastIndexOf(".") + 1)];
                MQTT_Labsterium.instance.SendMQTTMessageToTopic("DEBUG/CONVERT", "Converting " + nameFile);
                var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ffmpeg",
                        Arguments = "-y -i " + path + nameVideo + " -c:v libvpx -c:a libvorbis -crf 4 -b:v 3M " + path + nameFile + ".webm",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                proc.OutputDataReceived += ConvertDataReceived;
                proc.ErrorDataReceived += ConvertDataReceived;
                proc.Exited += EndConvertion;
                MQTT_Labsterium.instance.SendMQTTMessageToTopic("DEBUG/CONVERT", "Starting process");
                proc.EnableRaisingEvents = true;
                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                MQTT_Labsterium.instance.SendMQTTMessageToTopic("DEBUG/CONVERT", "Process started");
            }
            catch (System.Exception e)
            {
                MQTT_Labsterium.instance.SendMQTTMessageToTopic("ERROR", e.ToString());
            }
        }
    }

    private void EndConvertion(object sender, EventArgs e)
    {
        MQTT_Labsterium.instance.SendMQTTMessageToTopic("DEBUG/CONVERT", "Process ended");

    }

    private void ConvertDataReceived(object sender, DataReceivedEventArgs e)
    {
        try
        {
            if (e.Data.Contains("Duration"))
            {
                var duration = e.Data[(e.Data.IndexOf(":") + 2)..e.Data.IndexOf(",")];
                UnityEngine.Debug.Log(duration);
                UnityEngine.Debug.Log(duration.Length);
                var d = TimeSpan.Parse(duration);
                maxDuration = d;
                // var d = TimeSpan.ParseExact(duration, "hh:mm:ss:tt", System.Globalization.CultureInfo.InvariantCulture);
                // sliderProgress.maxValue = d.Ticks;
            }
            else if (e.Data.Contains("time="))
            {
                var duration = e.Data[(e.Data.IndexOf("time=") + 5)..e.Data.IndexOf("bitrate")];
                UnityEngine.Debug.Log(duration);
                var d = TimeSpan.Parse(duration);
                convertedDuration = d;
                MQTT_Labsterium.instance.SendMQTTMessageToTopic("DEBUG/CONVERT", "Convert : " + convertedDuration.TotalSeconds + "/" + maxDuration.TotalSeconds + " s");

                // var d = TimeSpan.ParseExact(duration, "hh:mm:ss:tt", System.Globalization.CultureInfo.InvariantCulture);
                // sliderProgress.value = d.Ticks;
            }

        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.Log(ex.ToString());
            MQTT_Labsterium.instance.SendMQTTMessageToTopic("DEBUG", ex.ToString());
        }
    }

    public void ListMedias()
    {
        var s = "LIST_[";
        foreach (var file in Directory.GetFiles(Helper.GetFilePath()))
        {
            var idx = file.LastIndexOf(Path.DirectorySeparatorChar) + 1;
            s += file[idx..] + ",";
        }
        s = s[..^1] + ']';
        MQTT_Labsterium.instance.SendMQTTMessage(s);
    }
}
