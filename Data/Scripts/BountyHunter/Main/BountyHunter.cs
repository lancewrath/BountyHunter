using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.Components;
using VRageMath;
using Sandbox.ModAPI;
using VRage.ModAPI;
using VRage.Game.ModAPI;

namespace RazMods.Hunter
{
    using RazMods.Hunter.Spawns;
    using RazMods.Hunter.Stations;


    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class BountyHunter : MySessionComponentBase
    {
        #region vars
        BountySpawnManager bountySpawnManager;
        StationManager stationManager;



        bool bIsServer = false;
        bool bInitialized = false;
        #endregion


        #region Init

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            
            base.Init(sessionComponent);
            Initialize();
        }

        public void Initialize()
        {
            bountySpawnManager = new BountySpawnManager();
            stationManager = new StationManager();
            bIsServer = MyAPIGateway.Multiplayer.IsServer;
            bInitialized = true;

            if (!bIsServer)
                return;
        }

        public override void BeforeStart()
        {
            base.BeforeStart();
            MyAPIGateway.Utilities.ShowMessage("Bounty:", "Initialized");
            HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(entities);           
            stationManager.Setup();

        }

        public void SetCallbacks()
        {
            MyAPIGateway.Entities.OnEntityAdd += CheckGrid;
            MyAPIGateway.Entities.OnEntityAdd += stationManager.Entities_OnEntityAdd;
            MyAPIGateway.Entities.OnEntityRemove += Entities_OnEntityRemove;

            
        }



        private void Entities_OnEntityRemove(IMyEntity entity)
        {
            if (entity == null)
                return;

            MyAPIGateway.Entities.OnEntityAdd -= stationManager.Entities_OnEntityAdd;
            MyAPIGateway.Entities.OnEntityAdd -= CheckGrid;
            MyAPIGateway.Entities.OnEntityRemove -= Entities_OnEntityRemove;
        }

        #endregion


        public override void UpdateBeforeSimulation()
        {
            if (!bIsServer)
                return;

            base.UpdateBeforeSimulation();
        }

        public void CheckGrid(IMyEntity entity)
        {
            if (entity == null)
                return;

            if (entity as IMyCubeGrid != null)
            {
                var grid = entity as IMyCubeGrid;
                if (grid != null)
                {

                }
            }
        }

        public static double MeasureDistance(Vector3D coordsStart, Vector3D coordsEnd)
        {

            double distance = Math.Round(Vector3D.Distance(coordsStart, coordsEnd), 2);
            return distance;

        }

    }
}
