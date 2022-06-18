using Sandbox.Game;
using Sandbox.Game.SessionComponents;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Interfaces;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.ObjectBuilders;
using VRageMath;

namespace RazMods.Hunter.CutScenes
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation, 900, typeof(MyObjectBuilder_CutsceneSessionComponent), null, false)]
    public class CutSceneManager : MySessionComponentBase
    {
        public List<CutSceneData> cutScenes = new List<CutSceneData>();
        public MyObjectBuilder_CutsceneSessionComponent cutsceneSession;
        private MyObjectBuilder_CutsceneSessionComponent m_objectBuilder;
        private Dictionary<string, Cutscene> m_cutsceneLibrary = new Dictionary<string, Cutscene>();

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            //Store waypoints in this way
            //MyVisualScriptLogicProvider.StoreEntityVectorList("ScoutWaypoints")
            base.Init(sessionComponent);
            m_objectBuilder = (sessionComponent as MyObjectBuilder_CutsceneSessionComponent);
            if (m_objectBuilder == null || m_objectBuilder.Cutscenes == null || m_objectBuilder.Cutscenes.Length == 0)
            {
                
                return;
            }
            MyAPIGateway.Utilities.ShowMessage("CutScene:", m_objectBuilder.ToString());
            Cutscene[] cutscenes = m_objectBuilder.Cutscenes;
            foreach (Cutscene cutscene in cutscenes)
            {
                if (cutscene.Name != null && cutscene.Name.Length > 0 && !m_cutsceneLibrary.ContainsKey(cutscene.Name))
                {
                    MyAPIGateway.Utilities.ShowMessage("Scene:", cutscene.Name);
                    m_cutsceneLibrary.Add(cutscene.Name, cutscene);
                }
            }


        }


        public void AddCutScene(IMyEntity entity,string cutsceneName, string waypointsName,List<CutsceneSequenceNodeWaypoint> waypointdef, List<Vector3D> waypoints)
        {

            List<Vector3D> vwaypoints = new List<Vector3D>();


            CutSceneData cutSceneData = new CutSceneData();
            cutSceneData.entityID = entity.EntityId;
            cutSceneData.cutscene = new Cutscene();
            cutSceneData.cutscene.Name = cutsceneName;
            cutSceneData.cutscene.SequenceNodes = new List<CutsceneSequenceNode>();
            cutScenes.Add(cutSceneData);
            MyVisualScriptLogicProvider.StoreEntityVectorList(entity.Name, waypointsName, waypoints);
            CutsceneSequenceNode cutsceneSequenceNode = new CutsceneSequenceNode();
            cutsceneSequenceNode.AttachTo = entity.Name;
            cutsceneSequenceNode.SetPositionTo = entity.Name;
            cutsceneSequenceNode.Time = 55;
            cutsceneSequenceNode.Waypoints = waypointdef;

        }

    }

    public enum CutSceneType
    {
        ORBIT,
        CHASE,
        EVENT
    }

    public class CutSceneData
    {
        public long entityID = 0;
        public CutSceneType sceneType = CutSceneType.ORBIT;
        public Cutscene cutscene = null;
        
    }
}
