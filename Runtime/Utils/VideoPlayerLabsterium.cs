using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using System.IO;
using System;
using Labsterium;
using System.Threading.Tasks;
using System.Diagnostics;
using UnityEngine.UI;
using UnityEngine.Events;

public class VideoInfo
{
    public string nameVideo;
    public double time;
    public bool log;
    public int n;
    public bool loop;
    public float volume;
    public VideoInfo returnTo;
}

public class VideoPlayerLabsterium : MonoBehaviour
{
    VideoConverter converter;
    public RawImage imgObj;
    public VideoPlayer vp;
    VideoInfo currentVideoInfo;
    string path;
    public UnityEvent<string> videoEnd;
    void Start()
    {
        converter = new VideoConverter();
        currentVideoInfo = null;
        vp.loopPointReached += EndReached;
        Application.runInBackground = true;
        path = Helper.GetFilePath();
    }

    public bool ProcessMessageForVideoPlayer(string method, List<string> args)
    {
        switch (method)
        {
            case "PLAY":
                {
                    return Play(args);
                }
            case "PLAYNOTIFICATION":
                {
                    return Play(args, false, false, true);
                }
            case "PLAYLOOPLOG":
                {
                    return Play(args, true, true);
                }
            case "PLAYLOOP":
                {
                    return Play(args, true, false);
                }
            case "PAUSE":
                {
                    vp.Pause();
                    return true;
                }
            case "STOP":
                {
                    imgObj.gameObject.SetActive(false);
                    vp.Stop();
                    currentVideoInfo = null;
                    return true;
                }
            case "SHOW":
                {
                    return Show(args);
                }
            case "SKIP":
                {
                    vp.frame = (int)vp.frameCount - Mathf.RoundToInt(3f * vp.frameRate);
                    return true;
                }
            case "LIST":
                {
                    converter.ListMedias();
                    return true;
                }
            case "CONVERT":
                {
                    converter.ConvertVideo(args);
                    return true;
                }

        }
        return false;
    }
    void Connected()
    {
        converter.Connected();
    }

    private bool Show(List<string> args)
    {
        if (args.Count == 0)
        {
            MQTT_Labsterium.instance.SendMQTTMessage("ERROR_NO_FILE");
            return false;
        }
        return Show(args[0]);


    }

    public bool Show(string file)
    {
        imgObj.gameObject.SetActive(true);
        var imgPath = path + file;
        if (!File.Exists(imgPath))
        {
            MQTT_Labsterium.instance.SendMQTTMessage("ERROR_FILE_NOT_FOUND");
            return false;
        }
        var bytes = File.ReadAllBytes(imgPath);
        var texture = new Texture2D(2, 2);
        texture.LoadImage(bytes);
        imgObj.texture = texture;
        return true;
    }
    private bool Play(List<string> args, bool loop = false, bool log = true, bool notification = false)
    {
        if (args.Count == 0)
        {
            if (currentVideoInfo != null)
            {
                imgObj.gameObject.SetActive(false);
                vp.Play();
                return true;
            }
            else
            {
                MQTT_Labsterium.instance.SendMQTTMessage("ERROR_NO_FILE_PLAYING");
                return false;
            }
        }
        return Play(args[0], loop, log, notification);
    }
    public bool Play(string file, bool loop = false, bool log = true, bool notification = false)
    {

        imgObj.gameObject.SetActive(false);

        bool isNotif = currentVideoInfo != null && notification;
        if (isNotif)
        {
            currentVideoInfo.time = vp.time;
        }
        var nvi = new VideoInfo()
        {
            time = 0,
            log = log,
            loop = loop,
            n = 0,
            nameVideo = file,
            volume = 1f,
            returnTo = isNotif ? currentVideoInfo : null,
        };
        if (!File.Exists(path + nvi.nameVideo))
        {
            MQTT_Labsterium.instance.SendMQTTMessage("ERROR_FILE_NOT_FOUND");
            return false;
        }
        currentVideoInfo = nvi;
        vp.url = path + nvi.nameVideo;
        vp.isLooping = loop;
        vp.Play();

        return true;
    }
    void EndReached(VideoPlayer vp)
    {
        if (currentVideoInfo.loop)
        {
            currentVideoInfo.n++;
            if (currentVideoInfo.log)
                MQTT_Labsterium.instance.SendMQTTMessage("LOOP_" + currentVideoInfo.n + "_DONE");
        }
        else
        {
            if (currentVideoInfo.returnTo != null)
            {
                MQTT_Labsterium.instance.SendMQTTMessage("VIDEO_" + currentVideoInfo.nameVideo + "_FINISHED");
                currentVideoInfo = currentVideoInfo.returnTo;
                vp.url = path + currentVideoInfo.nameVideo;
                vp.SetDirectAudioVolume(0, currentVideoInfo.volume);
                vp.Play();
                ReturnToTime(currentVideoInfo.time);

            }
            else
            {
                MQTT_Labsterium.instance.SendMQTTMessage("VIDEO_" + currentVideoInfo.nameVideo + "_FINISHED");
                currentVideoInfo = null;
            }
        }
    }
    async void ReturnToTime(double t)
    {
        while (!vp.isPlaying) await Task.Delay(5);
        vp.time = t;
    }
}
