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
                        this.def = gdef;
                        MyLog.Default.WriteLineAndConsole("Grid Generator: Loaded - " + gdef.Name);
                        CompilePrefabs(gdef);
                        generatorDefs.Add(gdef);
                    }
                }
            }
            if(def==null)
                def = new GeneratorDef();

        }


        void CompilePrefabs(GeneratorDef generatorDef)
        {


            foreach (var item in generatorDef.battery)
            {

                MyDefinitionManager.Static.GetPrefabDefinition(item);
            }
            foreach (var item in generatorDef.ceiling)
            {
                MyDefinitionManager.Static.GetPrefabDefinition(item);
            }
            foreach (var item in generatorDef.chair)
            {
                MyDefinitionManager.Static.GetPrefabDefinition(item);
            }
            foreach (var item in generatorDef.deco)
            {
                MyDefinitionManager.Static.GetPrefabDefinition(item);
            }
            foreach (var item in generatorDef.door)
            {
                MyDefinitionManager.Static.GetPrefabDefinition(item);
            }
            foreach (var item in generatorDef.floor)
            {
                MyDefinitionManager.Static.GetPrefabDefinition(item);
            }
            foreach (var item in generatorDef.gate)
            {
                MyDefinitionManager.Static.GetPrefabDefinition(item);
            }
            foreach (var item in generatorDef.functionals)
            {
                MyDefinitionManager.Static.GetPrefabDefinition(item);
            }
            foreach (var item in generatorDef.hangar)
            {
                MyDefinitionManager.Static.GetPrefabDefinition(item);
            }
            foreach (var item in generatorDef.lcargo)
            {
                MyDefinitionManager.Static.GetPrefabDefinition(item);
            }
            foreach (var item in generatorDef.scargo)
            {
                MyDefinitionManager.Static.GetPrefabDefinition(item);
            }
            foreach (var item in generatorDef.solidwall)
            {
                MyDefinitionManager.Static.GetPrefabDefinition(item);
            }
            foreach (var item in generatorDef.stairs)
            {
                MyDefinitionManager.Static.GetPrefabDefinition(item);
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

            /*
           PrefabObject statmodel = new PrefabObject("OLD_FLOOR_1");
           string gendata = MyAPIGateway.Utilities.SerializeToXML(statmodel);
           TextWriter tw = MyAPIGateway.Utilities.WriteFileInWorldStorage("PrefabObject.xml", typeof(string));
           tw.Write(gendata);
           tw.Close();
            
           if(generator!=null)
           {
               string gendata = MyAPIGateway.Utilities.SerializeToXML(generator.stationMap);
               //string gendata = MyAPIGateway.Utilities.SerializeToXML(def);
               TextWriter tw = MyAPIGateway.Utilities.WriteFileInWorldStorage("MapData.xml", typeof(string));
               tw.Write(gendata);
               tw.Close();
           }
           */

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
