using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
namespace Labsterium
{
    public enum DebugLevel
    {
        NO_DEBUG,
        SCREEN_DEBUG,
        MQTT_DEBUG,
        BOTH_DEBUG
    }
    public class MQTTMessage
    {
        public string topic;
        public string message;
        public MQTTMessage()
        {
            topic = "";
            message = "";
        }
        public MQTTMessage(MqttMsgPublishEventArgs e)
        {
            topic = e.Topic;
            message = System.Text.Encoding.UTF8.GetString(e.Message);
        }
    }

    public class MQTT_Labsterium : MonoBehaviour
    {
        public bool mqttEnabled = true;
        public DebugLevel debugLevel;
        public static MQTT_Labsterium instance;
        TMPro.TextMeshProUGUI debug;
        public string ipAddr = "192.168.2.2";
        public int port = MqttSettings.MQTT_BROKER_DEFAULT_PORT;
        public List<string> topicsSub = new() { "DEVICES" };
        protected string basemecaName;
        public string mecaName = "UNITYMECA";
        public bool multiMeca;
        public bool alsoListenBaseName;
        protected string nMeca;
        public string targetTopic = "TOSERVER";
        public string receiveON = "DEVICES";
        protected MqttClient client;
        MQTTInfo mqttInfo;
        // string clientId = "";
        public bool logAllMessages;
        ConcurrentQueue<MQTTMessage> messageQueue;
        MQTTMessage message;
        public bool cursorVisible = false;
        protected void Awake()
        {
            mqttInfo = new()
            {
                clientid = ""
            };
            var go = Instantiate(new GameObject(), FindObjectOfType<Canvas>().transform);
            debug = go.AddComponent<TMPro.TextMeshProUGUI>();
            debug.color = Color.red;
            instance = this;
            basemecaName = mecaName;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
#if !UNITY_EDITOR
            Cursor.visible = cursorVisible;
#endif
            Application.runInBackground = true;
            if (multiMeca)
            {
                nMeca = File.ReadAllText("n.cfg");
                mecaName += nMeca;
            }
            Initialize();
        }
        protected void Update()
        {
            try
            {
                if (messageQueue.Count > 0)
                {
                    if (messageQueue.TryDequeue(out message))
                    {
                        ProcessMessage(message);
                    }
                }
            }
            catch (Exception)
            {
                Initialize();
            }

        }
        protected void OnDestroy()
        {
            DebugLab("Destroying !");
            if (client != null)
            {
                Publish("DISCONNECT", mecaName);
                client.Disconnect();
            }
        }
        protected void Initialize()
        {
            message ??= new MQTTMessage();
            messageQueue ??= new ConcurrentQueue<MQTTMessage>();
            if (client == null)
            {
                client = new MqttClient(IPAddress.Parse(ipAddr));
                client.MqttMsgPublishReceived += LogMsg;
                client.MqttMsgPublishReceived += OnMessage;
                client.MqttMsgDisconnected += OnDisconnect;
            }
            if (mqttInfo.clientid == "")
                mqttInfo.clientid = Guid.NewGuid().ToString();
            TryConnect();
        }
        async void TryConnect()
        {
            if (!mqttEnabled)
                return;
            if (client.IsConnected)
                return;
            DebugLab("Try connect");
            try
            {
                client.Connect(mqttInfo.clientid, null, null, false, MqttMsgConnect.QOS_LEVEL_AT_MOST_ONCE, true, "DISCONNECT", mecaName, true, 10);
            }
            catch (Exception e)
            {
                DebugLab(e);
                await Task.Delay(5000);
                TryConnect();
                throw;
            }
            byte[] qos = new byte[topicsSub.Count];
            for (int i = 0; i < topicsSub.Count; i++)
            {
                qos[i] = MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE;
            }
            client.Subscribe(topicsSub.ToArray(), qos);
            Publish("CONNECT", mecaName);
            DebugLab("Successfully connected to MQTT and subscribed to topics");
        }
        protected void OnDisconnect(object sender, EventArgs e)
        {
            DebugLab("Disconnected !");
            TryConnect();
        }
        protected virtual void OnMessage(object sender, MqttMsgPublishEventArgs e)
        {
            messageQueue.Enqueue(new MQTTMessage(e));
        }
        protected void LogMsg(object sender, MqttMsgPublishEventArgs e)
        {
            if (logAllMessages)
                DebugLab("Received: " + System.Text.Encoding.UTF8.GetString(e.Message));
        }
        public void Publish(string topic, string message)
        {
            if (!mqttEnabled)
                return;
            client.Publish(topic, System.Text.Encoding.ASCII.GetBytes(message));
        }
        public void SendMQTTMessage(string msg)
        {
            SendMQTTMessageToTopic(targetTopic, mecaName + "_" + msg);
        }
        public void SendMQTTMessageToTopic(string topic, string msg)
        {
            Publish(topic, msg);
        }
        protected virtual void ProcessMessage(MQTTMessage message)
        {
            if (message.topic == receiveON)
            {
                if (message.message == "IDENTIFICATION")
                {
                    Identify(false);
                    return;
                }
                var args = new List<string>(message.message.Split('_'));
                string destMeca = args[0];
                string method = args[1];
                args.RemoveRange(0, 2);
                if (destMeca == mecaName || (destMeca == basemecaName && alsoListenBaseName))
                {
                    if (ProcessMessageForDevice(method, args))
                    {
                        SendMQTTMessage(method + "_DONE");
                    }
                    else
                    {
                        SendMQTTMessage(method + "_INVALID");
                    }
                }
            }
        }
        protected virtual bool ProcessMessageForDevice(string method, List<string> args)
        {
            switch (method)
            {
                case "PING":
                    return true;
                case "SETDEBUG":
                    debugLevel = (DebugLevel)int.Parse(args[0]);
                    return true;
                case "INFOS":
                    {
                        Identify(true);
                        return true;
                    }
                case "QUIT":
                    {
                        Helper.Quit();
                        return true;
                    }
            }
            return false;
        }
        public void Identify(bool infos = false)
        {
            Task.Run(async () =>
            {
                try
                {
                    var ni = await Helper.GetNetworkInfo();
                    if (infos)
                    {
                        string infoString = "{\"" + mecaName + "\":{\"IP\":\"" + ni.IP + "\",\"RSSI\":\"" + (ni.RSSI).ToString() + "\"}}";
                        SendMQTTMessageToTopic("TOSERVER", infoString);
                    }
                    else
                    {
                        MecaInfo mi = new()
                        {
                            Network = ni,
                            MQTT = mqttInfo,
                            Hardware_Infos = new()
                            {
                                Type = "amd64"
                            }
                        };
                        string r = JsonUtility.ToJson(mi).ToString();
                        SendMQTTMessageToTopic("TOSERVER", r);
                    }
                }
                catch (Exception e)
                {
                    DebugLab(e);
                }
            }
            );
        }
        public static void DebugLab(object o)
        {
            Debug.Log(o);
            if (!mqttEnabled)
                return;
            if (instance.debugLevel == DebugLevel.NO_DEBUG)
                return;
            if (instance.debugLevel == DebugLevel.SCREEN_DEBUG || instance.debugLevel == DebugLevel.BOTH_DEBUG)
                instance.debug.text = o.ToString();
            if (instance.debugLevel == DebugLevel.MQTT_DEBUG || instance.debugLevel == DebugLevel.BOTH_DEBUG)
                instance.Publish(instance.mecaName + "/DEBUG", o.ToString());
        }
    }

}
