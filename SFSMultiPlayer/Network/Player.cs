using SFS.World;
using System;
using System.Net.Sockets;
using static SFSMultiPlayer.Network.Player;

namespace SFSMultiPlayer.Network
{
    public enum PlayerStatus
    {
        Online,
        Downline
    }

    public class Player
    {
        public string playerName = "Unkown Player";
        public int playerId = 0;
        public Socket socket;
        public Rocket rocket;
        public PlayerStatus connectStatus = PlayerStatus.Downline;
        public bool isMuted = false;

        public PlayerData PlayerData
        {
            get
            {
                return new PlayerData(playerName, connectStatus, isMuted);
            }
        }

        public Player()
        {

        }

        public Player(Socket socket)
        {
            this.socket = socket;
        }
    }

    [Serializable]
    public class PlayerData
    {
        public string playerName = "Unkown Player";
        public PlayerStatus connectStatus = PlayerStatus.Downline;
        public bool isMuted;

        public PlayerData()
        {

        }

        public PlayerData(string playerName, PlayerStatus connectStatus,bool isMuted)
        {
            this.playerName = playerName;
            this.connectStatus = connectStatus;
            this.isMuted = isMuted;
        }
    }
}
