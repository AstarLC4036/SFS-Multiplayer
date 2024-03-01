using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using static SFSMultiPlayer.Network.NetworkUtil;
using System.Collections.Concurrent;
using UnityEngine;

namespace SFSMultiPlayer.Network
{
    public class Client
    {
        public enum ClientState
        {
            None,
            Connected
        }

        public static Dictionary<MessageType, ClientCallBack> callbacks = new Dictionary<MessageType, ClientCallBack>();
        public static ConcurrentQueue<Callback> callBackQueue = new ConcurrentQueue<Callback>();

        public static Queue<byte[]> messages = new Queue<byte[]>();

        public static ClientState curState;
        public static float timer = 0;

        public static TcpClient client;
        public static NetworkStream stream;

        public static IPAddress address;

        public static int port = 8888;

        public static float HEARTBEAT_TIME = 3;
        public static bool receivedHeartBeat = true;

        public static RoomData room;
        public static PlayerData player;

        //初始化连接
        public static void InitConnect(string address = null, int port = 8888)
        {
            if (curState == ClientState.Connected)
            {
                return;
            }

            if (address == null)
            {
                address = GetLocalIPv4();
            }

            if (!IPAddress.TryParse(address, out Client.address))
            {
                return;
            }
        }

        //回调函数注册
        public static void Register(MessageType type, ClientCallBack method)
        {
            if (!callbacks.ContainsKey(type))
            {
                callbacks.Add(type, method);
            }
            else
            {
                Debug.Log("The same callback is registed");
            }
        }

        public static void Enquene(MessageType messageType, byte[] data = null)
        {
            //Debug.Log("Sending a data...");
            byte[] bytes = PackData(messageType, data);

            if(curState == ClientState.Connected)
            {
                messages.Enqueue(bytes);
            }
        }
    }
}
