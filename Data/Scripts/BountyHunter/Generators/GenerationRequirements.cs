using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RazMods.Hunter
{
    public class GenerationRequirements
    {
        public const int NO_LIMIT = -1;//Special "Unlimited" marker for the maximum model amount check.
        //Any number below 0 will work the same as this value

        public Dictionary<String, int> minimumModelRequirements;//Minimum amount of each model needed for map to be valid

        public Dictionary<String, int> maximumModelRequirements;//Maximum amount of each model needed for map to be valid

        public int minEndDistance, maxEndDistance; //Minimum and Maximum distance to the ENDING tile.

        public int minimumTileCount, maximumTileCount; //Minimum and Maximum overall tiles in the dungeon

        //Creates the default Generation Requirement without any limits
        public GenerationRequirements(List<StationModel> models)
        {
            minimumModelRequirements = new Dictionary<String, int>();
            maximumModelRequirements = new Dictionary<String, int>();
            minEndDistance = NO_LIMIT;
            maxEndDistance = NO_LIMIT;
            minimumTileCount = NO_LIMIT;
            maximumTileCount = NO_LIMIT;

            if (models != null && models.Count > 0)
            {
                foreach (StationModel m in models)
                {

                    minimumModelRequirements.Add(m.type, NO_LIMIT);
                    maximumModelRequirements.Add(m.type, NO_LIMIT);
                }
            }
        }


        public static bool requirementsMet(GenerationRequirements reqs, GeneratedStationInfo mapInfo)
        {

            //If requirements are null they are already met
            if (reqs == null)
                return true;

            bool met;

            int minc, maxc, mcc;//Model max / min count and current count

            //Loop to perform model checks if there are models to check

            if (mapInfo.modelTypeCount.Count > 0 && (reqs.maximumModelRequirements.Count > 0 || reqs.minimumModelRequirements.Count > 0))
            {
                foreach (String cmt in mapInfo.modelTypeCount.Keys)
                {

                    //Set variables from provided info and requirements

                    mapInfo.modelTypeCount.TryGetValue(cmt, out mcc);

                    reqs.minimumModelRequirements.TryGetValue(cmt, out minc);
                    reqs.maximumModelRequirements.TryGetValue(cmt, out maxc);

                    met = (mcc >= minc) && (((maxc > 0) && (mcc <= maxc)) || (maxc < 0));


                    //If a model's requirements are not met, return immediately
                    if (!met)
                    {
                        return false;
                    }

                }
            }

            //Next checking if the longest map path is withing the bounds set by the requirements

            met = ((reqs.minEndDistance > 0 && mapInfo.longestPath >= reqs.minEndDistance) || (reqs.minEndDistance < 0)) &&
                  ((reqs.maxEndDistance > 0 && mapInfo.longestPath <= reqs.maxEndDistance) || (reqs.maxEndDistance < 0));


            return met;
        }
    }
}
