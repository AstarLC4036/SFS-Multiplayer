using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;

namespace SFSMultiPlayer.Network
{
    public class NetworkUtil
    {
        public enum MessageType
        {
            None,
            DataPack,
            RocketDataPack,
            ChatPack,
            HeartBeat,

            //Player Operation
            Enroll,
            EnterRoom,
            ExitRoom,

            //Server Operation
            Remove,
            Mute,
            Close
        }

        public static byte[] Serializate(object obj)
        {
            if(obj == null || !obj.GetType().IsSerializable)
            {
                return null;
            }

            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream stream = new MemoryStream())
            {
                formatter.Serialize(stream, obj);
                byte[] data = stream.ToArray();
                return data;
            }
        }

        public static T Deserialize<T>(byte[] data) where T : class
        {
            if (data == null || !typeof(T).IsSerializable)
            {
                return null;
            }
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream stream = new MemoryStream(data))
            {
                object obj = formatter.Deserialize(stream);
                return obj as T;
            }
        }

        public static string GetLocalIPv4()
        {
            string hostName = Dns.GetHostName();
            IPHostEntry iPEntry = Dns.GetHostEntry(hostName);
            for (int i = 0; i < iPEntry.AddressList.Length; i++)
            {
                if (iPEntry.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
                    return iPEntry.AddressList[i].ToString();
            }
            return null;
        }

        public static byte[] PackData(MessageType type, byte[] data = null)
        {
            List<byte> list = new List<byte>();
            if (data != null)
            {
                list.AddRange(BitConverter.GetBytes((ushort)(4 + data.Length)));
                list.AddRange(BitConverter.GetBytes((ushort)type));
                list.AddRange(data);
            }
            else
            {
                list.AddRange(BitConverter.GetBytes((ushort)4));
                list.AddRange(BitConverter.GetBytes((ushort)type));
            }
            return list.ToArray();
        }
    }
}
