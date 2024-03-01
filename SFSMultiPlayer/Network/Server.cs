using System.Net.Sockets;
using System.Net;
using System.Threading;
using UnityEngine;
using static SFSMultiPlayer.Network.NetworkUtil;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;
using SFS.World;
using System.Linq;
using System.Collections;

namespace SFSMultiPlayer.Network
{
    public class Server
    {
        public static Socket socket;
        public static int port = 8888;

        public static Room room;
        public static Player hostPlayer = new Player();

        public static Queue<DoubleValueTable<RocketSave, Player>> rocketSaveLoads = new Queue<DoubleValueTable<RocketSave, Player>>();
        private static ConcurrentQueue<Callback> callBackQueue = new ConcurrentQueue<Callback>();

        private static Dictionary<MessageType, ServerCallBack> callBacks = new Dictionary<MessageType, ServerCallBack>();

        public static bool needUpdateGUI = false;
        private static bool isServerRunning = false;

        public static bool IsServerRunning
        {
            get
            {
                return isServerRunning;
            }
        }

        //启动服务器
        public static void Start(string ip)
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint point = new IPEndPoint(IPAddress.Parse(ip), port);
            socket.Bind(point);
            socket.Listen(0);

            hostPlayer.socket = socket;
            hostPlayer.playerId = room.players.Count + 1;
            room.players.Add(hostPlayer);

            isServerRunning = true;

            Thread playerJoinHandler = new Thread(HandleAwaitPlayer) { IsBackground = true };
            playerJoinHandler.Start();

            Thread handler = new Thread(HandleCallback) { IsBackground = true };
            handler.Start();
        }

        public static void Stop()
        {
            callBacks.Clear();
            isServerRunning = false;
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }

        //玩家加入处理
        public static void HandleAwaitPlayer()
        {
            while (isServerRunning)
            {
                Socket client = null;
                try
                {
                    client = socket.Accept();
                    string endPoint = client.RemoteEndPoint.ToString();

                    Player player = new Player(client);
                    player.connectStatus = PlayerStatus.Online;
                    player.playerId = room.players.Count + 1;
                    room.players.Add(player);

                    Debug.Log(player.socket.RemoteEndPoint.ToString() + " connected!");

                    ParameterizedThreadStart receiveMethod = new ParameterizedThreadStart(ReceivePlayerData);

                    Thread listener = new Thread(receiveMethod) { IsBackground = true };
                    listener.Start(room.players.Where(p => p == player).FirstOrDefault());
                }
                catch(Exception ex)
                {
                    Debug.Log(ex.Message);
                }
            }
        }

        //回调处理
        private static void HandleCallback()
        {
            while (isServerRunning)
            {
                if (callBackQueue.Count > 0)
                {
                    if (callBackQueue.TryDequeue(out Callback callBack))
                    {
                        callBack.ExecuteServer();
                    }
                }
                Thread.Sleep(10);
            }
        }

        //注册回调函数
        public static void Register(MessageType type, ServerCallBack method)
        {
            if (!callBacks.ContainsKey(type))
            {
                callBacks.Add(type, method);
            }
            else
            {
                Debug.Log("The same callback is registed");
            }
        }

        public static void ReceivePlayerData(object obj)
        {
            Player player = obj as Player;
            Socket client = player.socket;

            //循环不断接收
            while (isServerRunning)
            {
                if(player.connectStatus == PlayerStatus.Downline)
                {
                    room.players.ForEach((Player playerArg) =>
                    {
                        if (player.playerId == playerArg.playerId)
                            room.players.Remove(player);
                    });

                    needUpdateGUI = true;
                    Debug.Log(player.playerName + " downline");
                    return;
                }

                //解析数据包
                byte[] data = new byte[4];

                int length = 0;
                MessageType type = MessageType.None;
                int receive = 0;

                try
                {
                    receive = client.Receive(data);
                    //Debug.Log("Received data");
                }
                catch (Exception ex)
                {
                    Debug.Log($"{client.RemoteEndPoint} downline:{ex.Message}");
                    return;
                }

                //包头接收不完整
                if (receive < data.Length)
                {
                    Debug.Log($"{client.RemoteEndPoint} downline");
                    return;
                }

                //解析消息过程
                using (MemoryStream stream = new MemoryStream(data))
                {
                    BinaryReader binary = new BinaryReader(stream, Encoding.UTF8);
                    try
                    {
                        length = binary.ReadUInt16();
                        type = (MessageType)binary.ReadUInt16();
                    }
                    catch (Exception)
                    {
                        Debug.Log($"{client.RemoteEndPoint} downline");
                        return;
                    }
                }

                //如果有包体
                if (length - 4 > 0)
                {
                    data = new byte[length - 4];
                    receive = client.Receive(data);
                    if (receive < data.Length)
                    {
                        Debug.Log($"{client.RemoteEndPoint} downline");
                        return;
                    }
                }
                else
                {
                    data = new byte[0];
                    receive = 0;
                }

                //Console.WriteLine($"Received message, player count:{room.players.Count}");

                //回调机制机制
                if (callBacks.ContainsKey(type))
                {
                    Callback callBack = new Callback(player, data, callBacks[type]);
                    //放入回调队列
                    callBackQueue.Enqueue(callBack);
                }
            }
        }

        public static void SendData(Player player, MessageType type, byte[] data = null)
        {
            if(player == hostPlayer)
            {
                //Debug.Log("You can't send data to yourself");
                return;
            }
            byte[] bytes = PackData(type, data);

            //发送消息
            //Debug.Log("Connect status: " + player.socket.Connected);
            player.socket.Send(bytes);
        }
    }
}