using ModLoader;
using ModLoader.Helpers;
using SFS.World;
using SFSMultiPlayer.Network;
using Resources = UnityEngine.Resources;
using UnityEngine;
using System.Linq;
using SFS.Parsers.Json;

namespace SFSMultiPlayer
{
    public class Main : Mod
    {
        public override string Author => "AstarLC";
        public override string ModNameID => "multiplayer";
        public override string Description => "A simple mod for multiplayer";
        public override string DisplayName => "SFS MultiPlayer";
        public override string ModVersion => "1.0 Beta";
        public override string MinimumGameVersionNecessary => "1.5.9.8";

        public static bool inWorldSence = false;

        public override void Load()
        {
            SceneHelper.OnBuildSceneLoaded += () =>
            {
                ModUI.PrepareGUI();
                if (ModUI.windowState == ModUI.WindowState.None)
                {
                    ModUI.ShowMenuGUI();
                }
            };

            SceneHelper.OnWorldSceneLoaded += () =>
            {
                //Rocket[] rocketsT = (Rocket[])Resources.FindObjectsOfTypeAll(typeof(Rocket));
                //Rocket playerRocketT = null;
                //foreach (Rocket rocket in rocketsT)
                //{
                //    if (rocket.isPlayer)
                //    {
                //        playerRocketT = rocket;
                //    }
                //}

                //playerRocketT.rocketName = Server.hostPlayer.playerName;
                //string json = JsonWrapper.ToJson(new RocketSave(playerRocketT), false);
                //byte[] testData = NetworkUtil.Serializate(json);
                //string resultJson = NetworkUtil.Deserialize<string>(testData);
                //RocketSave resultData = JsonWrapper.FromJson<RocketSave>(resultJson);
                //bool test;
                //RocketManager.LoadRocket(resultData, out test);
                //Debug.Log(resultData.rocketName);

                ModUI.PrepareGUI();
                if (ModUI.windowState == ModUI.WindowState.None)
                {
                    ModUI.ShowMenuGUI();
                    ModUI.windowState = ModUI.WindowState.Menu;
                }
                else
                {
                    ModUI.RecoverUI();
                }


                inWorldSence = true;

                GameData.manager = GameObject.Find("Rocket Manager").GetComponent<RocketManager>();

                if (Client.curState == Client.ClientState.Connected)
                {
                    ClientNetwork.updateRocket = true;
                }

                if(Server.IsServerRunning)
                {
                    Rocket[] rockets = (Rocket[])Object.FindObjectsOfType(typeof(Rocket));
                    Rocket playerRocket = null;
                    foreach (Rocket rocket in rockets)
                    {
                        if (rocket.isPlayer)
                        {
                            playerRocket = rocket;
                        }
                    }
                    playerRocket.rocketName = Server.hostPlayer.playerName;
                    Server.hostPlayer.rocket = playerRocket;
                    Server.room.players.Where(p => p.playerId == Server.hostPlayer.playerId).FirstOrDefault().rocket = playerRocket;
                }
            };
            SceneHelper.OnWorldSceneUnloaded += () =>
            {
                inWorldSence = false;
            };
        }
    }
}
