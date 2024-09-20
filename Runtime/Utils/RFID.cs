using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Labsterium;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class RFID : MonoBehaviour
{
    string path;
    FileSystemWatcher fsw;
    Queue<string> messages;
    public UnityEvent<string> rfidEvent;
    // Start is called before the first frame update
    void Start()
    {
        messages = new Queue<string>();
        path = Directory.GetCurrentDirectory();
        fsw = new()
        {
            Path = path,
            NotifyFilter = NotifyFilters.LastWrite,
            Filter = "rfid.txt"
        };
        fsw.Changed += new FileSystemEventHandler(OnChanged);
        fsw.EnableRaisingEvents = true;
    }
    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        var s = File.ReadAllText(e.FullPath);
        messages.Enqueue(s);
        Debug.Log(s);
    }
    void Update()
    {
        while (messages.Count > 0)
        {
            var v = messages.Dequeue();
            rfidEvent.Invoke(v);
        }
    }
}
