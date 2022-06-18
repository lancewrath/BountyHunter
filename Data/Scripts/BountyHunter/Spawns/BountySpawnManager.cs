using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;

namespace RazMods.Hunter.Spawns
{
    public class BountySpawnManager
    {
        public static BountySpawnManager main = null;
        public List<IMyCubeGrid> managedGrids = new List<IMyCubeGrid>();
        public List<IMyCubeGrid> markedForDeletion = new List<IMyCubeGrid>();
        public BountySpawner bountySpawner = null;

        public BountySpawnManager()
        {
            bountySpawner = new BountySpawner();
            bountySpawner.SetupSpawns();
            main = this;
        }



        public void Update()
        {

            ProcessRemoval();
        }

        public void ProcessRemoval()
        {
            IMyCubeGrid deletegrid = markedForDeletion.FirstOrDefault();
            if (deletegrid != null)
            {
                markedForDeletion.Remove(deletegrid);
                //unlink callbacks
                deletegrid.Delete();

            }
        }

    }

    [System.Serializable]
    public class BountyGrids
    {
        public List<BountyGrid> bountyGrids = new List<BountyGrid>();
    }

    [System.Serializable]
    public class BountyGrid
    {
        public long entityID;
        public long targetID;
        public string name;

    }
}
