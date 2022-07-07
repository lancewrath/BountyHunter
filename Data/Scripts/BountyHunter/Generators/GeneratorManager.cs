using Sandbox.Definitions;
using Sandbox.Game.GameSystems.BankingAndCurrency;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Components.Session;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace RazMods.Hunter
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class GeneratorManager : MySessionComponentBase
    {
        List<GeneratorDef> generatorDefs = new List<GeneratorDef>();
        public static GeneratorManager Instance;
        public bool isGenerating = false;
        GeneratorDef def;
        List<StationModel> stationModels = new List<StationModel>();
        GridGenerator generator = null;
        bool bIsServer = false;
        bool bInitialized = false;

        public void GenerateGrid(string GeneratorName = "Default Generator")
        {
            GeneratorDef generatorDef = generatorDefs.Find(g => g.Name == GeneratorName);
            if (generatorDef == null)
                generatorDef = new GeneratorDef();

            if (generator != null) return;

            generator = new GridGenerator(generatorDef.seed, generatorDef.width, generatorDef.height, generatorDef.maxFloors, stationModels, generatorDef);
            isGenerating = true;
            generator.GeneratedStation();

        }

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            GeneratorManager.Instance = this;
            base.Init(sessionComponent);
            bIsServer = MyAPIGateway.Multiplayer.IsServer;
            bInitialized = true;


            if (!bIsServer)
                return;


            ListReader<MyComponentDefinitionBase> entComps = MyDefinitionManager.Static.GetEntityComponentDefinitions();
            foreach (var def in entComps)
            {
                if (def.Id.SubtypeId.String.Contains("[GeneratorDef]"))
                {
                    string gendef = def.DescriptionString;
                    gendef = gendef.Replace("]", ">");
                    gendef = gendef.Replace("[", "<");
                    GeneratorDef gdef = MyAPIGateway.Utilities.SerializeFromXML<GeneratorDef>(gendef);
                    if (gdef != null)
                    {
                        //this.def = gdef;                        
                        CompilePrefabs(gdef);
                        generatorDefs.Add(gdef);
                        MyLog.Default.WriteLineAndConsole("Grid Generator: Loaded - " + gdef.Name);
                    }
                }
            }
           // if(def==null)
            //    def = new GeneratorDef();

        }

        public override void UpdateBeforeSimulation()
        {
            if (!bIsServer)
                return;

            base.UpdateBeforeSimulation();
        }

        void RemapGrids(string prefab)
        {
            MyPrefabDefinition prefabDef = MyDefinitionManager.Static.GetPrefabDefinition(prefab);
            if (prefabDef != null)
            {
                MyObjectBuilder_CubeGrid[] cgrids = prefabDef.CubeGrids;
                foreach (var cgrid in cgrids)
                {
                    MyAPIGateway.Entities.RemapObjectBuilder(cgrid);
                }
            }
        }

        void CompilePrefabs(GeneratorDef generatorDef) 
        {


            foreach (var item in generatorDef.battery)
            {
                RemapGrids(item);
            }
            foreach (var item in generatorDef.ceiling)
            {
                RemapGrids(item);
            }
            foreach (var item in generatorDef.chair)
            {
                RemapGrids(item);
            }
            foreach (var item in generatorDef.deco)
            {
                RemapGrids(item);
            }
            foreach (var item in generatorDef.door)
            {
                RemapGrids(item);
            }
            foreach (var item in generatorDef.floor)
            {
                RemapGrids(item);
            }
            foreach (var item in generatorDef.gate)
            {
                RemapGrids(item);
            }
            foreach (var item in generatorDef.functionals)
            {
                RemapGrids(item);
            }
            foreach (var item in generatorDef.hangar)
            {
                RemapGrids(item);
            }
            foreach (var item in generatorDef.lcargo)
            {
                RemapGrids(item);
            }
            foreach (var item in generatorDef.scargo)
            {
                RemapGrids(item);
            }
            foreach (var item in generatorDef.solidwall)
            {
                RemapGrids(item);
            }
            foreach (var item in generatorDef.stairs)
            {
                RemapGrids(item);
            }
            foreach (var item in generatorDef.light)
            {
                RemapGrids(item);
            }
            foreach (var item in generatorDef.reactor)
            {
                RemapGrids(item);
            }
            foreach (var item in generatorDef.lcd1)
            {
                RemapGrids(item);
            }
            foreach (var item in generatorDef.lcd2)
            {
                RemapGrids(item);
            }
            foreach (var item in generatorDef.entrance)
            {
                RemapGrids(item);
            }
        }


        public override void SaveData()
        {
            base.SaveData();

            if (def != null)
            {

                string gendata = MyAPIGateway.Utilities.SerializeToXML(def);
                TextWriter tw = MyAPIGateway.Utilities.WriteFileInWorldStorage("MapData.xml", typeof(string));
                tw.Write(gendata);
                tw.Close();
            }


        }
    }



    [System.Serializable]
    public class GeneratorDef
    {
        public string Name = "Default Generator";
        public int seed = 123;
        public int itemSeed = 123;
        public int width = 20, height = 20;
        public bool randomSeedMode = false;
        public int roomsize = 2;
        public int roomrand = 4;
        public int maxFloors = 2;
        public float Branch = 0.325f;
        public float CorridorTurn =  0.3f;
        public float CorridorEnd = 0.005f;
        public float Room = 0.25f;
        public float Stairs = 0.03f;
        public float Model = 0.195f;
        public float Csavoid = 0.502f;
        public bool Planar = true;
        public bool Priority = false;
        public bool CleanDeadEnds = true;
        public float EndRooms = 0.7f;
        public int CorridorMaxLength = 75;
        public float TallRooms = 0.4f;
        public int TallRoomMaxHeight = 6;
        public int TallRoomMinHeight = 3;
        public float CorridorFromRooms = 0.686f;
        public float LargeCorridors = 0.483f;
        public bool UseRequirements = true;
        public int roommin = 1;
        public int roommax = 2;
        public Vector3D nodeSize = new Vector3D(7.5, 7.5, 7.5);
        public float floorHeight = 3;
        public List<string> floor = new List<string>(), ceiling = new List<string>(), solidwall = new List<string>(), door = new List<string>(), lcargo = new List<string>(), scargo = new List<string>(), reactor = new List<string>(), battery = new List<string>(), functionals = new List<string>(), hangar = new List<string>(), gate = new List<string>(), light = new List<string>(), lcd1 = new List<string>(), lcd2 = new List<string>(), stairs = new List<string>(), chair = new List<string>(), deco = new List<string>(), entrance = new List<string>();

    }
}
