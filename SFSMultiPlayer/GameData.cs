using Newtonsoft.Json;
using SFS.Parts;
using SFS.Parts.Modules;
using SFS.World;
using System;
using System.Linq;
using UnityEngine;

namespace SFSMultiPlayer
{
    public class GameData
    {
        public static RocketManager manager = null;
    }

    public class RocketUtil
    {
        public static Rocket LoadRocket(RocketSave rocketSave)
        {
            Rocket rocket;

            //From decomplied source code
            OwnershipState[] source;
            Part[] parts = PartsLoader.CreateParts(rocketSave.parts, null, null, OnPartNotOwned.Allow, out source);
            rocket = GameObject.Instantiate(GameData.manager.rocketPrefab);
            rocket.rocketName = rocketSave.rocketName;
            rocket.isPlayer.Value = false;
            rocket.throttle.throttleOn.Value = rocketSave.throttleOn;
            rocket.throttle.throttlePercent.Value = rocketSave.throttlePercent;
            rocket.arrowkeys.rcs.Value = rocketSave.RCS;
            rocket.SetJointGroup(new JointGroup((from a in rocketSave.joints select new PartJoint(parts[a.partIndex_A], parts[a.partIndex_B], parts[a.partIndex_B].Position - parts[a.partIndex_A].Position)).ToList(), parts.ToList()));
            rocket.rb2d.transform.eulerAngles = new Vector3(0f, 0f, rocketSave.rotation);
            rocket.physics.SetLocationAndState(rocketSave.location.GetSaveLocation(WorldTime.main.worldTime), false);
            rocket.rb2d.angularVelocity = rocketSave.angularVelocity;
            rocket.staging.Load(rocketSave.stages, rocket.partHolder.GetArray(), false);
            rocket.staging.editMode.Value = rocketSave.staging_EditMode;
            rocket.stats.Load(rocketSave.branch);

            return rocket;
        }
    }

    [Serializable]
    public class RocketDataSync
    {
        public string name = "Rocket";
        public bool RCS = false;
        public bool throttleOn = false;
        public float throttlePercent = 0;
        public float rotationZ;
        public WorldSave.LocationData location;

        public RocketDataSync()
        {

        }

        public RocketDataSync(Rocket rocket)
        {
            name = rocket.name;
            RCS = rocket.arrowkeys.rcs.Value;
            throttleOn = rocket.throttle.throttleOn;
            throttlePercent = rocket.throttle.throttlePercent;
            rotationZ = rocket.rb2d.transform.eulerAngles.z;
            location = new WorldSave.LocationData(rocket.location.Value);
        }
    }

    [Serializable]
    public class RocketData
    {
        public string rocketName;
        public WorldSave.LocationData location;
        public float rotation;
        public float angularVelocity;
        public bool throttleOn;
        public float throttlePercent;
        public bool RCS;
        public PartSave[] parts;
        public JointSave[] joints;
        public StageSave[] stages = new StageSave[0];
        public bool staging_EditMode;
        public int branch = -1;

        public RocketData()
        {

        }
        public RocketData(Rocket rocket)
        {
            this.rocketName = rocket.rocketName;
            this.location = new WorldSave.LocationData(rocket.location.Value);
            this.rotation = rocket.rb2d.transform.eulerAngles.z;
            this.angularVelocity = rocket.rb2d.angularVelocity;
            this.throttleOn = rocket.throttle.throttleOn;
            this.throttlePercent = rocket.throttle.throttlePercent;
            this.RCS = rocket.arrowkeys.rcs;
            this.parts = PartSave.CreateSaves(rocket.partHolder.GetArray());
            this.joints = JointSave.CreateSave(rocket);
            this.stages = StageSave.CreateSaves(rocket.staging, rocket.partHolder.parts);
            this.staging_EditMode = rocket.staging.editMode;
            this.branch = rocket.stats.branch;
        }

        public RocketSave ToRocketSave()
        {
            RocketSave rocketSave = new RocketSave();
            rocketSave.rocketName = rocketName;
            rocketSave.location = location;
            rocketSave.location = location;
            rocketSave.angularVelocity = angularVelocity;
            rocketSave.throttleOn = throttleOn;
            rocketSave.throttlePercent = throttlePercent;
            rocketSave.RCS = RCS;
            rocketSave.parts = parts;
            rocketSave.joints = joints;
            rocketSave.stages = stages;
            rocketSave.staging_EditMode = staging_EditMode;
            rocketSave.branch = branch;
            return rocketSave;
        }
    }
}
