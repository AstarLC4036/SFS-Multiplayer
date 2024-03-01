using SFS.World;
using System.Collections;
using System.Linq;
using SFS.Parts;
using SFS.Parts.Modules;
using UnityEngine;
using System.Collections.Generic;
using SFS.Parsers.Json;

namespace SFSMultiPlayer.Network
{
    public class ServerMainInstance : MonoBehaviour
    {
        static ServerMainInstance instance;

        public static ServerMainInstance Instance
        {
            get
            {
                if(instance == null)
                {
                    GameObject serverMainGameObject = new GameObject("Server Main");
                    instance = serverMainGameObject.AddComponent<ServerMainInstance>();
                    DontDestroyOnLoad(instance);
                    Debug.Log("Server Instance Created");
                    return instance;
                }
                else
                {
                    return instance;
                }
            }
        }

        public static void StartLoadCoroutine()
        {
            Instance.StartCoroutine("MainCoroutine");
        }

        public IEnumerator MainCoroutine()
        {
            Debug.Log("Running load rocket");
            while(true)
            {
                if(Server.needUpdateGUI)
                {
                    ModUI.UpdateServerGUI();
                    Server.needUpdateGUI = false;
                }

                if(Server.rocketSaveLoads.Count > 0 && Main.inWorldSence)
                {
                    //Get player data and rocket data from equeue
                    DoubleValueTable<RocketSave, Player> datas = Server.rocketSaveLoads.Dequeue();
                    RocketSave rocketSave = datas.Value1;
                    Player client = datas.Value2;

                    //If load rocket data on a thread, the game will crash.
                    Rocket rocket = RocketUtil.LoadRocket(rocketSave);

                    Server.room.players.Where(p => p == client).LastOrDefault().rocket = rocket;

                    //Debug.Log("Loaded rocket");

                    List<RocketSave> rocketSaves = new List<RocketSave>();

                    Server.room.players.ForEach((Player player) =>
                    {
                        if (player.rocket == null)
                        {
                            return;
                        }
                        rocketSaves.Add(new RocketSave(player.rocket));
                    });

                    List<RocketSave> rocketSavesDir = rocketSaves;

                    Server.room.players.ForEach((Player player) =>
                    {
                        rocketSavesDir.ForEach((RocketSave rocketDataDir) =>
                        {
                            if (rocketDataDir == new RocketSave(player.rocket))
                            {
                                rocketSavesDir.Remove(rocketDataDir);
                            }
                        });

                        if (player.playerId != Server.hostPlayer.playerId)
                        {
                            string jsonData = JsonWrapper.ToJson(rocketSavesDir, false);
                            byte[] rocketDirSaves = NetworkUtil.Serializate(jsonData);
                            Server.SendData(player, NetworkUtil.MessageType.RocketDataPack, rocketDirSaves);
                            rocketSavesDir = rocketSaves;
                        }
                    });
                }
                yield return null;
            }
        }
    }
}
