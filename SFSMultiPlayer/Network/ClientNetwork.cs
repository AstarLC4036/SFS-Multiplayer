using System;
using System.Collections;
using System.Net.Sockets;
using static SFSMultiPlayer.Network.Client;
using UnityEngine;
using static SFSMultiPlayer.Network.NetworkUtil;
using System.IO;
using System.Text;
using UnityEngine.Events;
using Resources = UnityEngine.Resources;
using SFS.World;
using SFS.Builds;
using SFS.WorldBase;
using SFS.UI;
using SFS.Parsers.Json;
using System.Collections.Generic;

namespace SFSMultiPlayer.Network
{
    public class ClientNetwork : MonoBehaviour
    {
        public static bool updateRocket = false;

        static ClientNetwork instance;
        static UnityAction<bool> connectCallback;
        static PlayerData connectPlayerData;
        static string connectIP;
        static int connectPort = -1;

        public static ClientNetwork Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject instanceObj = new GameObject("Network Coroutine");
                    instance = instanceObj.AddComponent<ClientNetwork>();
                    DontDestroyOnLoad(instanceObj);
                    return instance;
                }
                else
                {
                    return instance;
                }
            }
        }

        public void ConnectServer(UnityAction<bool> callback, PlayerData playerData, string ip, int port)
        {
            //Regist callbacks
            Register(MessageType.EnterRoom, EnterRoom);
            Register(MessageType.ExitRoom, ExitRoom);
            Register(MessageType.DataPack, DataPack);
            Register(MessageType.Mute, Mute);
            Register(MessageType.Remove, Remove);
            Register(MessageType.HeartBeat, HeartBeat);
            Register(MessageType.ChatPack, ChatPack);
            Register(MessageType.RocketDataPack, RocketDataPack);

            //Arguments
            connectCallback = callback;
            connectPlayerData = playerData;
            player = playerData;
            connectIP = ip;
            connectPort = port;

            //Start connect
            instance.StartCoroutine("Connect");
        }

        //To make 'stream' isn't a null value when these coroutines are running
        private void StartStreamCoroutines()
        {
            instance.StartCoroutine("ReceiveData");
            instance.StartCoroutine("SendData");
            instance.StartCoroutine("HandleCallback");
            instance.StartCoroutine("HeartBeatPack");
            instance.StartCoroutine("DataPackCoro");
        }

        public void QuitServer()
        {
            Enquene(MessageType.ExitRoom, null);
            curState = ClientState.None;
            player.connectStatus = PlayerStatus.Downline;
            callbacks.Clear();
            client.Close();
            client = null;
            stream = null;
            Debug.Log("Quit server");
        }
        
        //Callback methods
        private static void EnterRoom(byte[] data)
        {
            Debug.Log("Received 'EnterRoom'");
            RoomData room = Deserialize<RoomData>(data);
            Client.room = room;

            if(!ModUI.isClientDataGUIBuilded)
            {
                ModUI.BuildClientData();
            }
            else
            {
                ModUI.UpdateClientGUI();
            }
        }

        private static void ExitRoom(byte[] data)
        {

        }

        private static void ChatPack(byte[] data)
        {
            Debug.Log("Received 'Chat'");
            string chatString = Deserialize<string>(data);
            Debug.Log(chatString);
        }

        private static void Mute(byte[] data)
        {
            string isMuted = Deserialize<string>(data);
            if(isMuted == "True")
            {
                Debug.Log("You have been muted by server admin");
                player.isMuted = true;
            }
            else
            {
                Debug.Log("You have been unmute by server admin");
                player.isMuted = false;
            }
        }

        private static void Remove(byte[] data)
        {
            Debug.Log("You have been removed from server by server admin");
            ModUI.QuitServer();
        }

        private static void DataPack(byte[] data)
        {

        }

        private static void RocketDataPack(byte[] data)
        {
            string jsonData = Deserialize<string>(data);
            List<RocketSave> rocketSaves = JsonWrapper.FromJson<List<RocketSave>>(jsonData);
            rocketSaves.ForEach((RocketSave save) =>
            {
                RocketUtil.LoadRocket(save);
            });
        }

        private static void HeartBeat(byte[] data)
        {
            //Debug.Log("Received 'HeartBeat'");
            receivedHeartBeat = true;
        }

        //与服务器连接并且获取Stream来传输和接收数据
        //Connect to server(socket) and get the NetworkStream to rend and receive the data.
        public IEnumerator Connect()
        {
            //Connect to server
            client = new TcpClient();

            IAsyncResult async;
            if (connectIP == null || connectPort == -1)
            {
                async = client.BeginConnect(address, port, null, null);
            }
            else
            {
                async = client.BeginConnect(connectIP, connectPort, null, null);
            }
            
            while (!async.IsCompleted)
            {
                Debug.Log("Connecting...");
                yield return null;
            }

            client.EndConnect(async);

            //Get NetworkStream
            stream = client.GetStream();

            //Set status
            curState = ClientState.Connected;
            Debug.Log("Connect successed!");

            //Call callback method
            connectCallback(true);

            //Send join room data pack to server
            byte[] data = Serializate(connectPlayerData);
            Enquene(MessageType.EnterRoom, data);

            //Run all NetworkStream coroutine
            StartStreamCoroutines();
        }

        public IEnumerator HandleCallback()
        {
            Debug.Log("Start 'HandleCallback'");
            while (curState == ClientState.Connected)
            {
                if (callBackQueue.Count > 0)
                {
                    if (callBackQueue.TryDequeue(out Callback callBack))
                    {
                        callBack.ExecuteClient();
                    }
                }
                yield return null;
            }
        }

        public IEnumerator HeartBeatPack()
        {
            while (curState == ClientState.Connected)
            {
                timer += Time.deltaTime;
                if (timer >= HEARTBEAT_TIME)
                {
                    if (!receivedHeartBeat)
                    {
                        //curState = ClientState.None;
                        Debug.Log("No heart beat pack, downline");
                        yield break;
                    }
                    timer = 0;
                    receivedHeartBeat = false;
                    byte[] data = PackData(MessageType.HeartBeat);
                    //Debug.Log("The heart beat pack has been sent.");
                    yield return Write(data);
                }
                yield return null;
            }
        }

        public IEnumerator DataPackCoro()
        {
            while (curState == ClientState.Connected)
            {
                Rocket[] rockets = FindObjectsOfType<Rocket>();
                Rocket targetRocket = null;
                foreach (Rocket rocket in rockets)
                {
                    if (rocket.isPlayer.Value)
                    {
                        targetRocket = rocket;
                    }
                }
                while (updateRocket)
                {
                    if (Main.inWorldSence)
                    {
                        try
                        {
                            
                            if (targetRocket != null)
                            {
                                targetRocket.rocketName = player.playerName;
                                string jsonData = JsonWrapper.ToJson(new RocketSave(targetRocket), false);
                                byte[] data = Serializate(jsonData);
                                Enquene(MessageType.RocketDataPack, data);
                                updateRocket = false;
                                break;
                            }
                        }
                        catch (Exception e) 
                        {
                            Debug.Log("Send rocket full data failed: " + e.Message);
                        }
                    }
                }

                if (Main.inWorldSence)
                {
                    byte[] data = Serializate(new RocketDataSync(targetRocket));
                    Enquene(MessageType.DataPack, data);
                }

                yield return new WaitForSeconds(0.01f);
            }
        }
        
        public IEnumerator SendData()
        {
            while (curState == ClientState.Connected)
            {
                if (messages.Count > 0)
                {
                    byte[] data = messages.Dequeue();
                    yield return Write(data);
                }
                yield return null;
            }
        }

        private static IEnumerator Write(byte[] data)
        {
            //Debug.Log("Sending a data pack...");
            //如果服务器下线, 客户端依然会继续发消息
            if (curState != ClientState.Connected || stream == null)
            {
                Debug.Log("Disconnected from server");
                yield break;
            }

            //异步发送消息
            IAsyncResult async = stream.BeginWrite(data, 0, data.Length, null, null);
            while (!async.IsCompleted)
            {
                yield return null;
            }
            //异常处理
            try
            {
                stream.EndWrite(async);
            }
            catch (Exception ex)
            {
                curState = ClientState.None;
                Debug.Log("Disconnected from server" + ex.Message);
            }
        }

        public IEnumerator ReceiveData()
        {
            Debug.Log("Start 'ReceiveData'");
            while (curState == ClientState.Connected)
            {
                //Debug.Log("Receiving data...");
                byte[] data = new byte[4];

                int length = 0;
                MessageType type = MessageType.None;
                int receive = 0;

                IAsyncResult async = stream.BeginRead(data, 0, data.Length, null, null);
                while (!async.IsCompleted)
                {
                    yield return null;
                }
                try
                {
                    receive = stream.EndRead(async);
                }
                catch(Exception e)
                {
                    Debug.Log("Receive data pack failed: " + e.Message);
                    continue;
                }
                using (MemoryStream stream = new MemoryStream(data))
                {
                    BinaryReader binary = new BinaryReader(stream, Encoding.UTF8);

                    try
                    {
                        length = binary.ReadUInt16();
                        type = (MessageType)binary.ReadUInt16();
                    }
                    catch (Exception e)
                    {
                        Debug.Log("Receive pack head failed: " + e.Message);
                        continue;
                    }
                }

                //如果有包体
                //Debug.Log("Received pack length and type");
                if (length - 4 > 0)
                {
                    data = new byte[length - 4];
                    //异步读取
                    async = stream.BeginRead(data, 0, data.Length, null, null);
                    while (!async.IsCompleted)
                    {
                        yield return null;
                    }
                    //异步读取完毕
                    receive = stream.EndRead(async);
                }
                //没有包体
                else
                {
                    data = new byte[0];
                    receive = 0;
                }

                //Debug.Log("Received a pack");

                if (callbacks.ContainsKey(type))
                {
                    Callback callBack = new Callback(data, callbacks[type]);
                    //放入回调队列
                    callBackQueue.Enqueue(callBack);
                }
            }
        }
    }
}