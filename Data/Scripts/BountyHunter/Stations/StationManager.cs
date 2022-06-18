using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRage.Game.ObjectBuilders.Definitions;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Localization;
using Sandbox.Game.World.Generator;
using VRage;
using Sandbox.Game.Contracts;
using Sandbox.Game.GameSystems.BankingAndCurrency;
using VRage.Game.ObjectBuilders.Components.Contracts;
using VRageMath;
using Sandbox.Game;
using VRage.ModAPI;

namespace RazMods.Hunter.Stations
{
    public class StationManager
    {
        public static StationManager main = null;
        public StationsData stations = null;
        
        public StationManager()
        {
            
            main = this;
            stations = new StationsData();
        }

        public void Setup()
        {
            GetStations();
            foreach(StationData station in stations.stations)
            {
                //add callbacks
                if(station.stationGrid != null)
                {
                    
                    station.stationGrid.OnClosing += StationGrid_OnClosing;
                    station.stationGrid.PlayerPresenceTierChanged += StationGrid_PlayerPresenceTierChanged;
                }
            }

        }

        #region CallBacks

        private void StationGrid_PlayerPresenceTierChanged(IMyCubeGrid obj)
        {

            StationData sData = stations.stations.Find(s => s.stationGrid == obj);
            if(sData != null)
            {
                if (obj.PlayerPresenceTier == MyUpdateTiersPlayerPresence.Normal)
                {
                    sData.isActive = true;
                } else
                {
                    sData.isActive = false;
                }

            }
        }

        private void StationGrid_OnClosing(IMyEntity obj)
        {
            if (obj as IMyCubeGrid != null)
            {
                IMyCubeGrid ent = obj as IMyCubeGrid;
                ent.PlayerPresenceTierChanged -= StationGrid_PlayerPresenceTierChanged;
            }
            
            obj.OnClosing -= StationGrid_OnClosing;
        }

        public void Entities_OnEntityAdd(IMyEntity entity)
        {
            if (entity == null)
                return;

            if (entity as IMyCubeGrid != null)
            {
                var grid = entity as IMyCubeGrid;
                if (grid != null)
                {
                    MyObjectBuilder_Station station;
                    TryGetStation(grid, out station);
                    if(station != null)
                    {
                        StationData sData = new StationData();
                        sData.station = station;
                        sData.stationGrid = grid;
                        stations.stations.Add(sData);
                    }
                }
            }
            
        }
        #endregion
        


        #region SaveLoad
        public void SetSaveData(CustomStations cstation)
        {
            if (cstation == null)
                return;

            stations.stations.Clear();
            foreach (CustomStation cs in cstation.Stations)
            {
                MyObjectBuilder_Station stationobj = GetStationByID(cs.stationID);
                if(stationobj != null)
                {
                    StationData sData = new StationData();
                    sData.station = stationobj;

                    IMyEntity ent = null;
                    if (MyAPIGateway.Entities.TryGetEntityById(sData.station.StationEntityId, out ent))
                    {
                        if (ent != null)
                        {
                            if (ent as IMyCubeGrid != null)
                            {
                                sData.stationGrid = ent as IMyCubeGrid;
                            }
                        }
                        
                    }
                    stations.stations.Add(sData);

                }

            }

            

        }
        #endregion

        #region GetandCheck
        public void GetStations()
        {
            List<MyObjectBuilder_Station> stationsList = GetBountyStations();
            foreach (MyObjectBuilder_Station stationobj in stationsList)
            {
                StationData sData = new StationData();
                sData.station = stationobj;
                IMyEntity ent = null;
                if (MyAPIGateway.Entities.TryGetEntityById(sData.station.StationEntityId, out ent))
                {
                    if (ent != null)
                    {
                        if (ent as IMyCubeGrid != null)
                        {
                            sData.stationGrid = ent as IMyCubeGrid;
                        }
                    }
                }
                stations.stations.Add(sData);
            }

        }

        public bool IsBountyFaction(IMyFaction fo)
        {
            if (fo == null)
                return false;

            var factionobj = MyAPIGateway.Session.Factions.GetObjectBuilder();
            if (factionobj != null)
            {
                var faction = factionobj.Factions.Find(f => f.FactionId == fo.FactionId);
                if (faction != null)
                {

                    if(faction.FactionType == MyFactionTypes.Miner)
                    {
                        if(faction.Name.Contains("[B]"))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public bool IsBountyFaction(MyObjectBuilder_Faction faction)
        {
            if (faction == null)
                return false;

            if (faction.FactionType == MyFactionTypes.Miner)
            {
                if (faction.Name.Contains("[B]"))
                {
                    return true;
                }
            }

            return false;
        }


        public List<MyObjectBuilder_Station> GetBountyStations()
        {
            List<MyObjectBuilder_Station> stations = new List<MyObjectBuilder_Station>();
            foreach (var fo in MyAPIGateway.Session.Factions.Factions)
            {
                stations.AddRange(GetFactionStations(fo.Value));
            }
            return stations;
        }

        public List<MyObjectBuilder_Station> GetFactionStations(IMyFaction fo)
        {
            List<MyObjectBuilder_Station> stations = new List<MyObjectBuilder_Station>();
            if(fo != null)
            {
                var factionobj = MyAPIGateway.Session.Factions.GetObjectBuilder();
                if(factionobj != null)
                {
                    var faction = factionobj.Factions.Find(f => f.FactionId == fo.FactionId);
                    if(faction != null)
                    {
                        
                        return faction.Stations;
                    } 
                }               
            }

            return stations;
        }

        public MyObjectBuilder_Station GetStationByID(long id)
        {
            foreach(var faction in MyAPIGateway.Session.Factions.Factions)
            {
                List<MyObjectBuilder_Station> stations = GetFactionStations(faction.Value);
                if(stations != null)
                {
                    MyObjectBuilder_Station sobj = stations.Find(s => s.Id == id);
                    if (sobj != null)
                        return sobj;
                }
            }
            return null;
        }

        public bool TryGetStation(IMyCubeGrid grid, out MyObjectBuilder_Station station)
        {

            if (grid == null)
            {
                station = null;
                return false;
            }

            var factionobj = MyAPIGateway.Session.Factions.GetObjectBuilder();
            if(factionobj!=null)
            {
                
                IMyFaction fo = MyAPIGateway.Session.Factions.TryGetPlayerFaction(grid.BigOwners.FirstOrDefault());
                if (fo != null)
                {
                    var faction = factionobj.Factions.Find(f => f.FactionId == fo.FactionId);
                    if(faction != null)
                    {
                        station = faction.Stations.Find(s => s.StationEntityId == grid.EntityId);
                        if (station != null)
                        {

                            return true;
                        }

                    }
                }
            }
            station = null;
            return false;
        }
        #endregion

    }

    #region HelperClasses

    public class StationsData
    {
        public List<StationData> stations = new List<StationData>();

        public CustomStations GetSaveData()
        {
            CustomStations cstations = new CustomStations();

            foreach (StationData station in stations)
            {
                CustomStation cs = new CustomStation();
                cs.factionID = station.station.FactionId;
                cs.stationID = station.station.Id;
                cs.entityID = station.station.StationEntityId;
                cstations.Stations.Add(cs);
            }

            return cstations;
        }
                              
    }

    public class StationData
    {
        public MyObjectBuilder_Station station;
        public IMyCubeGrid stationGrid;
        public bool isActive = false;
    }

    #endregion

    #region SerializableClasses

    [System.Serializable]
    public class CustomStations
    {
        public List<CustomStation> Stations = new List<CustomStation>();
    }

    [System.Serializable]
    public class CustomStation
    {
        public long stationID = 0;
        public long entityID = 0;
        public long factionID = 0;

    }

    #endregion
}
