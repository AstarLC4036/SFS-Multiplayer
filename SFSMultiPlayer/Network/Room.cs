using System;
using System.Collections.Generic;

namespace SFSMultiPlayer.Network
{
    public class Room
    {
        public string roomName = "Unnamed Room";
        public int roomId = 1;
        public int maxPlayers = 4;
        public List<Player> players = new List<Player>();

        public RoomData roomData
        {
            get
            {
                return new RoomData(roomName, roomId, maxPlayers, players.Count, players);
            }
        }

        public Room(string roomName, int roomId) 
        {
            this.roomName = roomName;
            this.roomId = roomId;
        }
    }

    [Serializable]
    public class RoomData
    {
        public string roomName = "Unnamed Room";
        public int roomId = 1;
        public int playerCount = 0;
        public int maxPlayers = 4;
        public List<PlayerData> players = new List<PlayerData>();

        public RoomData(string roomName, int roomId, int maxPlayers, int playerCount, List<Player> players)
        {
            this.roomName = roomName;
            this.roomId = roomId;
            this.maxPlayers = maxPlayers;
            this.playerCount = playerCount;
            players.ForEach((Player player) =>
            {
                this.players.Add(new PlayerData(player.playerName, player.connectStatus, player.isMuted));
            });
        }
    }
}