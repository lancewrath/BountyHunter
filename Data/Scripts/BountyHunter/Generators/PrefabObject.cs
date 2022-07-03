using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace RazMods.Hunter
{

    [System.Serializable]
    public class PrefabObjectListContainer
    {
        public List<PrefabObject> list = new List<PrefabObject>();
    }

    [System.Serializable]
    public class PrefabObject
    {

        public string prefabName;
        public bool selfAvoidance;
        public bool useChance;
        public bool autoDirectional;
        public float genChance;
        public float scaledChance;
        public NeighborRequirements genRequirements;
        public Boolean useAvoidanceList;
        public List<string> avoidanceList;
        public int globalPriority;
        public OrientationEnum orientation;
        public Boolean relativeTranslation;
        public Boolean relativeRotation;
        public Vector3 translation;
        public Vector4 oldTranslation;
        public Vector3 maxTranslation;
        public Vector3 minTranslation;
        public Vector3 rotation, maxRotation, minRotation;
        public Boolean toggle, randTransToggle, randRotToggle;
        public List<CustomObjectVariation> variations;
        public List<NeighborRequirements> extraPatterns;
        public PatternSelectionMode extraPatternMode;
        public bool AvoidanceDebug;


        public PrefabObject(string prefabname)
        {

            this.prefabName = prefabname;

            translation = Vector3.Zero;
            maxTranslation = Vector3.Zero;
            minTranslation = Vector3.Zero;
            minRotation = Vector3.Zero;
            maxRotation = Vector3.Zero;
            rotation = Vector3.Zero;
            this.genRequirements = new NeighborRequirements();
            extraPatterns = new List<NeighborRequirements>();
            extraPatterns.Add(new NeighborRequirements());
            this.extraPatternMode = PatternSelectionMode.All;
            this.AvoidanceDebug = false;
        }
        public PrefabObject()
        {
            this.prefabName = "";

            translation = Vector3.Zero;
            maxTranslation = Vector3.Zero;
            minTranslation = Vector3.Zero;
            minRotation = Vector3.Zero;
            maxRotation = Vector3.Zero;
            rotation = Vector3.Zero;
            this.genRequirements = new NeighborRequirements();
            extraPatterns = new List<NeighborRequirements>();
            extraPatterns.Add(new NeighborRequirements());
            this.extraPatternMode = PatternSelectionMode.All;
            this.AvoidanceDebug = false;
        }
        public void setTranslationV3(Vector3 v)
        {
            this.translation = v;
        }


    }
    [Serializable]
    public class CustomObjectVariation
    {

        public float chance;
        public string prefabName;

    }
    [System.Serializable]
    public enum PatternSelectionMode
    {

        Any, First, Last, All, All_Required


    }
}
