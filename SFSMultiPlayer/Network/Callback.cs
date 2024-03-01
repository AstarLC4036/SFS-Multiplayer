namespace SFSMultiPlayer.Network
{
    public delegate void ServerCallBack(Player client, byte[] data);
    public delegate void ClientCallBack(byte[] data);
    public class Callback
    {
        public Player Player;

        public Room Room;

        public byte[] Data;

        public ServerCallBack ServerCallBack;

        public ClientCallBack ClientCallBack;

        public Callback(Player player, byte[] data, ServerCallBack serverCallBack)
        {
            Player = player;
            Data = data;
            ServerCallBack = serverCallBack;
        }

        public Callback(byte[] data, ClientCallBack clientCallBack)
        {
            Data = data;
            ClientCallBack = clientCallBack;
        }

        public void ExecuteServer()
        {
            ServerCallBack(Player, Data);
        }

        public void ExecuteClient()
        {
            ClientCallBack(Data);
        }
    }
}
