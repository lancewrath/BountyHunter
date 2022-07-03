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
    using VRage.Utils;

    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class BountyHunter : MySessionComponentBase
    {
        #region vars
        BountySpawnManager bountySpawnManager;
        StationManager stationManager;
        GridGenerator gridGenerator;
 
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
            MyLog.Default.WriteLineAndConsole("Bounty Hunter: Initialized");
            MyAPIGateway.Utilities.ShowMessage("Bounty Hunter:", "Initialized");
            SetCallbacks();
        }

        public override void BeforeStart()
        {
            base.BeforeStart();

            HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(entities);           
            stationManager.Setup();

        }

        public void SetCallbacks()
        {
            MyAPIGateway.Entities.OnEntityAdd += CheckGrid;
            MyAPIGateway.Entities.OnEntityAdd += stationManager.Entities_OnEntityAdd;
            MyAPIGateway.Entities.OnEntityRemove += Entities_OnEntityRemove;
            //MyAPIGateway.Utilities.MessageRecieved += Utilities_MessageRecieved;
            MyAPIGateway.Utilities.MessageEnteredSender += Utilities_MessageEnteredSender;


        }

        private void Utilities_MessageEnteredSender(ulong sender, string messageText, ref bool sendToOthers)
        {
            if (messageText.Contains("/GenerateGrid") && !GeneratorManager.Instance.isGenerating)
            {
                MyAPIGateway.Utilities.ShowMessage("Bounty Hunter:", "Generating Grid");
                GeneratorManager.Instance.GenerateGrid();
                sendToOthers = false;
                
            }

        }

        private void Utilities_MessageRecieved(ulong playerid, string message)
        {
            MyAPIGateway.Utilities.ShowMessage("Message:", message);

        }

        private void Entities_OnEntityRemove(IMyEntity entity)
        {
            if (entity == null)
                return;


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
