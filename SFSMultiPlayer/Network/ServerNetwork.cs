using SFS.Parsers.Json;
using SFS.World;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using UnityEngine;
using static SFS.Sharing.SharingRequester;

namespace SFSMultiPlayer.Network
{
    public class ServerNetwork
    {
        public static void StartServer()
        {
            string ip = NetworkUtil.GetLocalIPv4();
            Server.Register(NetworkUtil.MessageType.Enroll, Enroll);
            Server.Register(NetworkUtil.MessageType.HeartBeat, HeartBeat);
            Server.Register(NetworkUtil.MessageType.EnterRoom, EnterRoom);
            Server.Register(NetworkUtil.MessageType.ExitRoom, ExitRoom);
            Server.Register(NetworkUtil.MessageType.ChatPack, ChatPack);
            Server.Register(NetworkUtil.MessageType.DataPack, DataPack);
            Server.Register(NetworkUtil.MessageType.RocketDataPack, RocketDataPack);
            Server.Start(ip);
        }

        public static void StopServer()
        {
            Server.Stop();
        }

        public static void Enroll(Player client, byte[] data)
        {

        }

        public static void HeartBeat(Player client, byte[] data)
        {
            //Debug.Log("Received 'HeartBeat'");
            Server.SendData(client, NetworkUtil.MessageType.HeartBeat, null);
        }

        public static void EnterRoom(Player client, byte[] data)
        {
            //Debug.Log("Received 'EnterRoom'");
            //Get PlayerData of new player;
            PlayerData playerData = NetworkUtil.Deserialize<PlayerData>(data);
            Debug.Log(playerData.playerName + " join the game");
            //Set player data to server room
            Server.room.players.ToArray()[Server.room.players.Count - 1].playerName = playerData.playerName;
            Server.room.players.ToArray()[Server.room.players.Count - 1].connectStatus = playerData.connectStatus;
            //Send room data to the sender client
            byte[] roomData = NetworkUtil.Serializate(Server.room.roomData);
            Server.SendData(client, NetworkUtil.MessageType.EnterRoom, roomData);
            //Update Local GUI
            ModUI.UpdateServerGUI();

            //Send new room data to all players
            Server.room.players.ForEach((Player player) =>
            {
                if (player != client && player != Server.hostPlayer)
                {
                    Server.SendData(player, NetworkUtil.MessageType.EnterRoom, roomData);
                    Server.SendData(player, NetworkUtil.MessageType.ChatPack, NetworkUtil.Serializate(client.playerName + " join the game"));
                }
            });
        }

        public static void ExitRoom(Player client, byte[] data)
        {
            Server.room.players.Where(p => p == client).LastOrDefault().connectStatus = PlayerStatus.Downline;
            //Send new room data to all players
            byte[] roomData = NetworkUtil.Serializate(Server.room.roomData);
            Server.room.players.ForEach((Player player) =>
            {
                if (player.playerId != client.playerId && player.playerId != Server.hostPlayer.playerId)
                {
                    Server.SendData(player, NetworkUtil.MessageType.EnterRoom, roomData);
                    Server.SendData(player, NetworkUtil.MessageType.ChatPack, NetworkUtil.Serializate(client.playerName + " quit the game"));
                }
            });
        }

        public static void DataPack(Player client, byte[] data)
        {
            RocketDataSync rocketData = NetworkUtil.Deserialize<RocketDataSync>(data);
            if(client.rocket != null)
            {
                Rocket rocket = Server.room.players.Where(p => p.playerId == client.playerId).FirstOrDefault().rocket;
                rocket.name = rocketData.name;
                rocket.arrowkeys.rcs.Value = rocketData.RCS;
                rocket.throttle.throttleOn.Value = rocketData.throttleOn;
                rocket.throttle.throttlePercent.Value = rocketData.throttlePercent;
                rocket.rb2d.transform.eulerAngles = new Vector3(0, 0, rocketData.rotationZ);
                rocket.physics.SetLocationAndState(rocketData.location.GetSaveLocation(WorldTime.main.worldTime), false);
            }
        }

        public static void RocketDataPack(Player client, byte[] data)
        {
            //Create a rocket
            //Rocket rocket;

            string rocketDataJson = NetworkUtil.Deserialize<string>(data);
            RocketSave rocketSave = JsonWrapper.FromJson<RocketSave>(rocketDataJson);
            Server.rocketSaveLoads.Enqueue(new DoubleValueTable<RocketSave, Player>(rocketSave, client));
        }

        public static void ChatPack(Player client, byte[] data)
        {
            //Receive message and send message to all players
            string message = NetworkUtil.Deserialize<string>(data);
            if(!client.isMuted)
            {
                Debug.Log(message);
            }
            Server.room.players.ForEach((Player player) =>
                {
                    if (player.playerId != client.playerId && !player.isMuted)
                    {
                        Server.SendData(player, NetworkUtil.MessageType.ChatPack, data);
                    }
                });
        }
    }
}
