using SFS.UI.ModGUI;
using Type = SFS.UI.ModGUI.Type;
using UnityEngine;
using SFSMultiPlayer.Network;
using static SFSMultiPlayer.Network.NetworkUtil;
using System.Collections.Generic;
using System.Reflection;
using Unity.Properties;

namespace SFSMultiPlayer
{
    public class ModUI
    {
        public static GameObject windowHolder;
        public static readonly int windowID = Builder.GetRandomID();
        public static Window windowMenu;
        public static Window windowCreateRoom;
        public static Window windowJoinRoom;
        public static Window windowServer;
        public static Window windowClient;
        public static Window windowPlayerListClient;
        public static Window windowPlayerListServer;
        public static RectInt windowRect = new RectInt(0, 0, 700, 800);

        //Public
        static string message;
        public static WindowState windowState = WindowState.None;
        public enum WindowState
        {
            None,
            Menu,
            JoinRoom,
            CreateRoom,
            Server,
            Client
        }

        //Server
        static string hostPlayerName = "Player";
        static string roomName = "Unnamed room";
        static int roomPort = 8888;
        static int roomId = 1;
        static TextInput textInputServer;
        static Label playerCountServer;

        //Client
        static string clientPlayerName = "Player";
        static string serverIP = "127.0.0.1";
        static int serverPort = 8888;
        static TextInput textInputClient;
        static Label playerCountClient;
        static Box clientDataBox;
        public static bool isClientDataGUIBuilded = false;

        //show create menu
        public static void PrepareGUI()
        {
            windowHolder = Builder.CreateHolder(Builder.SceneToAttach.CurrentScene, "MultiPlayerMod");
            windowMenu = Builder.CreateWindow(windowHolder.transform, windowID, windowRect.width, windowRect.height, windowRect.x, windowRect.y, true, true, 0.9f, "Multi-player Mod");
            windowMenu.CreateLayoutGroup(Type.Vertical);
            Builder.CreateButtonWithLabel(windowMenu, 650, 50, 0, 0, "Create a room", "Create", CreateRoomBtnEvent);
            Builder.CreateButtonWithLabel(windowMenu, 650, 50, 0, 25, "Join a room", "Join", JoinRoomBtnEvent);

            windowCreateRoom = Builder.CreateWindow(windowHolder.transform, windowID, windowRect.width, windowRect.height, windowRect.x, windowRect.y, true, true, 0.9f, "Create Room");
            windowCreateRoom.CreateLayoutGroup(Type.Vertical);
            Builder.CreateInputWithLabel(windowCreateRoom, 650, 50, 0, 0, "Player name", "Player", (string name) => { hostPlayerName = name; });
            Builder.CreateInputWithLabel(windowCreateRoom, 650, 50, 0, 25, "Room name", "Unnamed room", (string name) => { roomName = name; });
            Builder.CreateInputWithLabel(windowCreateRoom, 650, 50, 0, 25, "Room port", "8888", (string port) => { roomPort = int.Parse(port); });
            Builder.CreateInputWithLabel(windowCreateRoom, 650, 50, 0, 25, "Room Id", "1", (string id) => { roomId = int.Parse(id); });
            Builder.CreateButton(windowCreateRoom, 400, 50, 0, 50, CreateRoom, "Create");
            Builder.CreateButton(windowCreateRoom, 400, 50, 0, 100, () => { windowMenu.rectTransform.position = windowCreateRoom.rectTransform.position;  windowCreateRoom.gameObject.SetActive(false); windowMenu.gameObject.SetActive(true); windowState = WindowState.Menu; }, "Back");

            windowJoinRoom = Builder.CreateWindow(windowHolder.transform, windowID, windowRect.width, windowRect.height, windowRect.x, windowRect.y, true, true, 0.9f, "Join Room");
            windowJoinRoom.CreateLayoutGroup(Type.Vertical);
            Builder.CreateInputWithLabel(windowJoinRoom, 650, 50, 0, 0, "Player name", "Player", (string name) => { clientPlayerName = name; });
            Builder.CreateInputWithLabel(windowJoinRoom, 650, 50, 0, 25, "Server IP", "127.0.0.1", (string ip) => { serverIP = ip; });
            Builder.CreateInputWithLabel(windowJoinRoom, 650, 50, 0, 25, "Server port", "8888", (string port) => { serverPort = int.Parse(port); });
            Builder.CreateButton(windowJoinRoom, 400, 50, 0, 25, JoinRoom, "Join");
            Builder.CreateButton(windowJoinRoom, 400, 50, 0, 75, () => { windowMenu.rectTransform.position = windowJoinRoom.rectTransform.position; windowJoinRoom.gameObject.SetActive(false); windowMenu.gameObject.SetActive(true); windowState = WindowState.Menu; }, "Back");

            windowMenu.gameObject.SetActive(false);
            windowCreateRoom.gameObject.SetActive(false);
            windowJoinRoom.gameObject.SetActive(false);
        }

        private static void PrepareServerGUI()
        {
            windowServer = Builder.CreateWindow(windowHolder.transform, windowID, windowRect.width, windowRect.height, windowRect.x, windowRect.y, true, true, 0.9f, "Multi-Player Mod - Server");
            windowServer.CreateLayoutGroup(Type.Vertical);
            Box serverDataBox = Builder.CreateBox(windowServer, 600, 350, 0, 0, 0.3f);
            serverDataBox.CreateLayoutGroup(Type.Vertical);
            Builder.CreateLabel(serverDataBox, 400, 50, 0, 0, "Server IP: " + NetworkUtil.GetLocalIPv4()).FontSize = 10;
            Builder.CreateLabel(serverDataBox, 400, 50, 0, 0, "Server Port: " + roomPort).FontSize = 10;
            Builder.CreateLabel(serverDataBox, 400, 50, 0, 0, "Player name: " + Server.hostPlayer.playerName).FontSize = 10;
            playerCountServer = Builder.CreateLabel(serverDataBox, 400, 50, 0, 0, "Player count: " + Server.room.players.Count + "/" + Server.room.maxPlayers);
            playerCountServer.FontSize = 10;
            Builder.CreateSeparator(windowServer, 580, 0, 10);
            Container sendMessageContainer = Builder.CreateContainer(windowServer, 0, 20);
            sendMessageContainer.CreateLayoutGroup(Type.Horizontal);
            textInputServer = Builder.CreateInputWithLabel(sendMessageContainer, 490, 50, 0, 0, "Input Message", "", GetMessage).textInput;
            Builder.CreateButton(sendMessageContainer, 90, 50, 0, 0, SendMessageServer, "Send");
            //Builder.CreateButton(windowServer, 400, 50, 0, 0, () => { Server.room.players.ForEach((Player player) => { Server.SendData(player, MessageType.HeartBeat, null); }) ; }, "Send heat beat pack(Test)");
            Builder.CreateButton(windowServer, 400, 50, 0, 0, () => { ResetPlayerListGUIServer(Server.room.players); }, "Player List");
            Builder.CreateButton(windowServer, 400, 50, 0, 0, CloseServer, "Close Server");

            windowServer.gameObject.SetActive(false);
        }

        private static void PrepareClientGUI()
        {
            windowClient = Builder.CreateWindow(windowHolder.transform, windowID, windowRect.width, windowRect.height, windowRect.x, windowRect.y, true, true, 0.9f, "Multi-Player Mod - Client");
            windowClient.CreateLayoutGroup(Type.Vertical);
            clientDataBox = Builder.CreateBox(windowClient, 600, 200, 0, 0, 0.3f);
            Builder.CreateSeparator(windowClient, 580, 0, 10);
            Container sendMessageContainer = Builder.CreateContainer(windowClient, 0, 20);
            sendMessageContainer.CreateLayoutGroup(Type.Horizontal);
            textInputClient = Builder.CreateInputWithLabel(sendMessageContainer, 490, 50, 0, 0, "Input Message", "", GetMessage).textInput;
            Builder.CreateButton(sendMessageContainer, 90, 50, 0, 0, SendMessageClient, "Send");
            //Builder.CreateButton(windowClient, 400, 50, 0, 25, () => { Client.Enquene(MessageType.HeartBeat, null); }, "Send heart beat pack(Test)");
            Builder.CreateButton(windowClient, 400, 50, 0, 0, () => { ResetPlayerListGUIClient(Client.room.players); }, "Player List");
            Builder.CreateButton(windowClient, 400, 50, 0, 75, QuitServer, "Quit");

            windowClient.gameObject.SetActive(false);
        }

        public static void ResetPlayerListGUIServer(List<Player> players)
        {
            if(windowPlayerListServer != null)
            {
                Object.Destroy(windowPlayerListServer.gameObject);
                windowPlayerListServer = null;
            }

            windowPlayerListServer = Builder.CreateWindow(windowHolder.transform, windowID, windowRect.width, 200 + 60 * players.Count, windowRect.x, windowRect.y, true, true, 0.9f, "Multi-Player Mod - Player List");
            windowPlayerListServer.CreateLayoutGroup(Type.Vertical);
            windowPlayerListServer.rectTransform.position = windowServer.rectTransform.position;
            int index = 0;
            foreach (Player player in players)
            {
                index += 1;
                Container container = Builder.CreateContainer(windowPlayerListServer, 0, 0);
                container.CreateLayoutGroup(Type.Horizontal, TextAnchor.MiddleLeft, 5);
                string labelText = "[" + index + "] " + player.playerName;
                Label playerNameLabel = Builder.CreateLabel(container, 20 + 40 * labelText.Length <= 600 ? 20 + 40 * labelText.Length : 600, 60, 0, 0, labelText);
                Builder.CreateSpace(container, 500 - (20 + 40 * labelText.Length <= 450 ? 20 + 40 * labelText.Length : 450), 50);
                playerNameLabel.TextAlignment = TMPro.TextAlignmentOptions.MidlineLeft;
                playerNameLabel.AutoFontResize = false;
                playerNameLabel.FontSize = 30;

                if (player != Server.hostPlayer)
                {
                    string muteBtnText = player.isMuted ? "Unmute" : "Mute";
                    Button btnMute = Builder.CreateButton(container, 70, 50, 0, 0, () => { player.isMuted = !player.isMuted; Server.SendData(player, MessageType.Mute, Serializate(player.isMuted.ToString())); ResetPlayerListGUIServer(Server.room.players); }, muteBtnText);
                    Button BtnRemove = Builder.CreateButton(container, 70, 50, 0, 0, () => { Server.SendData(player, MessageType.Remove, null); ServerNetwork.ExitRoom(player, null); }, "Remove");
                }
            }
            Builder.CreateButton(windowPlayerListServer, 100, 50, 0, 0, () => { windowPlayerListServer.gameObject.SetActive(false); }, "Close");
        }

        public static void ResetPlayerListGUIClient(List<PlayerData> players)
        {
            if (windowPlayerListClient != null)
            {
                Object.Destroy(windowPlayerListClient.gameObject);
                windowPlayerListClient = null;
            }

            windowPlayerListClient = Builder.CreateWindow(windowHolder.transform, windowID, windowRect.width, 200 + 60 * players.Count, windowRect.x, windowRect.y, true, true, 0.9f, "Multi-Player Mod - Player List");
            windowPlayerListClient.CreateLayoutGroup(Type.Vertical);
            windowPlayerListClient.rectTransform.position = windowClient.rectTransform.position;
            int index = 0;
            foreach (PlayerData player in players)
            {
                index += 1;
                string labelText = "[" + index + "] " + player.playerName;
                Label playerNameLabel = Builder.CreateLabel(windowPlayerListClient, 600, 50, 0, 0, labelText);
                playerNameLabel.TextAlignment = TMPro.TextAlignmentOptions.MidlineLeft;
                playerNameLabel.AutoFontResize = false;
                playerNameLabel.FontSize = 30;
            }
            Builder.CreateButton(windowPlayerListClient, 100, 50, 0, 0, () => { windowPlayerListClient.gameObject.SetActive(false); }, "Close");
        }

        //Add client data to client data box if received room data
        public static void BuildClientData()
        {
            clientDataBox.CreateLayoutGroup(Type.Vertical);
            Builder.CreateLabel(clientDataBox, 400, 50, 0, 0, "Room name: " + Client.room.roomName).FontSize = 10;
            playerCountClient = Builder.CreateLabel(clientDataBox, 400, 50, 0, 0, "Player Count: " + Client.room.playerCount + "/" + Client.room.maxPlayers);
            playerCountClient.FontSize = 10;
        }

        public static void RecoverUI()
        {
            switch (windowState)
            {
                case WindowState.Menu:
                    ShowMenuGUI();
                    windowState = WindowState.Menu;
                    break;
                case WindowState.JoinRoom:
                    windowJoinRoom.gameObject.SetActive(true);
                    windowState = WindowState.JoinRoom;
                    break;
                case WindowState.CreateRoom:
                    windowCreateRoom.gameObject.SetActive(true);
                    windowState = WindowState.CreateRoom;
                    break;
                case WindowState.Server:
                    PrepareServerGUI();
                    windowServer.gameObject.SetActive(true);
                    windowState = WindowState.Server;
                    break;
                case WindowState.Client:
                    PrepareClientGUI();
                    BuildClientData();
                    windowClient.gameObject.SetActive(true);
                    windowState = WindowState.Client;
                    break;
            }
        }

        public static void ShowMenuGUI()
        {
            windowMenu.gameObject.SetActive(true);
            windowState = WindowState.Menu;
        }

        public static void CreateRoomBtnEvent()
        {
            //Debug.Log("Create");
            windowCreateRoom.rectTransform.position = windowMenu.rectTransform.position;
            windowMenu.gameObject.SetActive(false);
            windowCreateRoom.gameObject.SetActive(true);
            windowState = WindowState.CreateRoom;
        }

        public static void JoinRoomBtnEvent()
        {
            //Debug.Log("Join");
            windowJoinRoom.rectTransform.position = windowMenu.rectTransform.position;
            windowMenu.gameObject.SetActive(false);
            windowJoinRoom.gameObject.SetActive(true);
            windowState = WindowState.JoinRoom;
        }

        public static void CreateRoom()
        {
            Server.port = roomPort;
            Server.room = new Room(roomName, roomId);
            Server.hostPlayer.playerName = hostPlayerName;
            ServerNetwork.StartServer();
            ServerMainInstance.StartLoadCoroutine();
            PrepareServerGUI();
            windowServer.rectTransform.position = windowCreateRoom.rectTransform.position;
            windowCreateRoom.gameObject.SetActive(false);
            windowServer.gameObject.SetActive(true);
            windowState = WindowState.Server;
        }

        public static void JoinRoom()
        {
            PlayerData playerData = new PlayerData();
            playerData.playerName = clientPlayerName;
            playerData.connectStatus = PlayerStatus.Online;
            playerData.isMuted = false;

            Client.InitConnect();
            ClientNetwork.Instance.ConnectServer((bool isSuccessed) => { 
                if(!isSuccessed)
                {
                    windowMenu.gameObject.SetActive(true);
                    return;
                }
                PrepareClientGUI();
                windowClient.rectTransform.position = windowCreateRoom.rectTransform.position;
                windowJoinRoom.gameObject.SetActive(false);
                windowClient.gameObject.SetActive(true);
                windowState = WindowState.Client;
            }, playerData, serverIP, serverPort);
        }

        public static void GetMessage(string message)
        {
            ModUI.message = message;
        }

        public static void SendMessageServer()
        {
            string resultMsg = "[" + Server.hostPlayer.playerName + "]" + "[Server]: " + message;
            Debug.Log(resultMsg);
            byte[] data = Serializate(resultMsg);
            Server.room.players.ForEach((Player player) => { Server.SendData(player, MessageType.ChatPack, data); });
            textInputServer.Text = "";
        }

        public static void SendMessageClient()
        {
            string resultMsg = "[" + clientPlayerName + "][Client]: " + message;
            Debug.Log(resultMsg);
            if(Client.player.isMuted)
            {
                Debug.Log("You have been muted, other players can't receive you message");
            }
            byte[] data = Serializate(resultMsg);
            if (Client.curState == Client.ClientState.Connected)
            {
                Client.Enquene(MessageType.ChatPack, data);
            }
            textInputClient.Text = "";
        }

        public static void CloseServer()
        {
            ServerNetwork.StopServer();
            windowServer.gameObject.SetActive(false);
            windowMenu.gameObject.SetActive(true);
        }

        public static void QuitServer()
        {
            //Debug.Log("Quit");
            ClientNetwork.Instance.QuitServer();
            windowClient.gameObject.SetActive(false);
            windowMenu.gameObject.SetActive(true);
        }

        public static void UpdateServerGUI()
        {
            playerCountServer.Text = "Player count: " + Server.room.players.Count + "/" + Server.room.maxPlayers;
        }

        public static void UpdateClientGUI()
        {
            if(playerCountClient != null)
            {
                playerCountClient.Text = "Player Count: " + Client.room.playerCount + "/" + Client.room.maxPlayers;
            }
        }
    }
}