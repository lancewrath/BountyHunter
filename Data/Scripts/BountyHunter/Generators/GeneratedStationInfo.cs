using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace RazMods.Hunter
{

    public class GeneratedStationInfo
    {
        public Dictionary<string, int> modelTypeCount;//Used for demonstration

        public int longestPath;//Longest path on the map

        public Vector3D lastCorridor;//Last added corridor ( Position of the "Ending" tile )

        public int tilecount;

        public GeneratedStationInfo()
        {
            modelTypeCount = new Dictionary<string, int>();
            longestPath = 0;
            tilecount = 0;
            //Could not NULL the Vector3 because C# doesnt like that
        }

        public GeneratedStationInfo(List<StationModel> models)
        {
            modelTypeCount = new Dictionary<string, int>();
            longestPath = 0;
            tilecount = 0;

            if (models == null) return;

            foreach (StationModel m in models)
            {

                modelTypeCount.Add(m.type, 0);

            }

        }

        public void IncrementStationModelCount(string name)
        {
            int oldCount;

            if (modelTypeCount.ContainsKey(name))
            {
                modelTypeCount.TryGetValue(name, out oldCount);
                modelTypeCount.Remove(name);
                modelTypeCount.Add(name, ++oldCount);
            }
            else
            {
                modelTypeCount.Add(name, 1);
            }
        }

        public int GetStationModelCount(string name)
        {

            if (!modelTypeCount.ContainsKey(name))
            {
                return 0;
            }


            int val;
            modelTypeCount.TryGetValue(name, out val);
            return val;
        }
    }
}

