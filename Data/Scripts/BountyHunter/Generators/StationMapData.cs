using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RazMods.Hunter
{
    [System.Serializable]
    public class StationMapData
    {
        public StationTileType[,,] data;
        public int width, height, levels;
        public int seed;

        public PGTrigger[] triggers;

        private StationTileNeighborhood currentNeighborhood;

        public StationMapData() { }

        public StationMapData(StationTileType[,,] data, int w, int h, int l, int s)
        {
            this.data = data;
            this.width = w;
            this.height = h;
            this.levels = l;
            this.seed = s;
            this.triggers = null;
        }

        public StationMapData(StationTileType[,,] data, int w, int h, int l, int s, PGTrigger[] trigs)
        {
            this.data = data;
            this.width = w;
            this.height = h;
            this.levels = l;
            this.seed = s;
            this.triggers = trigs;
        }


        public StationTileNeighborhood getNeighbors(int x, int y, int l)
        {
            currentNeighborhood = new StationTileNeighborhood();

            if (data == null)
            {
                currentNeighborhood.north = StationTileType.ERROR;
                currentNeighborhood.south = StationTileType.ERROR;
                currentNeighborhood.east = StationTileType.ERROR;
                currentNeighborhood.west = StationTileType.ERROR;
                return currentNeighborhood;
            }

            try
            {
                currentNeighborhood.center = data[l, x, y];
            }
            catch
            {
                currentNeighborhood.center = StationTileType.ERROR;
            }

            try
            {
                currentNeighborhood.north = data[l, x, y + 1];
            }
            catch
            {
                currentNeighborhood.north = StationTileType.ERROR;
            }

            try
            {
                currentNeighborhood.south = data[l, x, y - 1];
            }
            catch
            {
                currentNeighborhood.south = StationTileType.ERROR;
            }

            try
            {
                currentNeighborhood.east = data[l, x + 1, y];
            }
            catch
            {
                currentNeighborhood.east = StationTileType.ERROR;
            }

            try
            {
                currentNeighborhood.west = data[l, x - 1, y];
            }
            catch
            {
                currentNeighborhood.west = StationTileType.ERROR;
            }
            return currentNeighborhood;
        }

        public StationTileNeighborhood getNeighbors3D(int x, int y, int l)
        {
            currentNeighborhood = new StationTileNeighborhood();

            if (data == null)
            {
                currentNeighborhood.north = StationTileType.ERROR;
                currentNeighborhood.south = StationTileType.ERROR;
                currentNeighborhood.east = StationTileType.ERROR;
                currentNeighborhood.west = StationTileType.ERROR;
                currentNeighborhood.above = StationTileType.ERROR;
                currentNeighborhood.below = StationTileType.ERROR;
                return currentNeighborhood;
            }

            try
            {
                currentNeighborhood.center = data[l, x, y];
            }
            catch
            {
                currentNeighborhood.center = StationTileType.ERROR;
            }

            try
            {
                currentNeighborhood.north = data[l, x, y + 1];
            }
            catch
            {
                currentNeighborhood.north = StationTileType.ERROR;
            }

            try
            {
                currentNeighborhood.south = data[l, x, y - 1];
            }
            catch
            {
                currentNeighborhood.south = StationTileType.ERROR;
            }

            try
            {
                currentNeighborhood.east = data[l, x + 1, y];
            }
            catch
            {
                currentNeighborhood.east = StationTileType.ERROR;
            }

            try
            {
                currentNeighborhood.west = data[l, x - 1, y];
            }
            catch
            {
                currentNeighborhood.west = StationTileType.ERROR;
            }

            try
            {
                currentNeighborhood.above = data[l + 1, x, y];
            }
            catch
            {
                currentNeighborhood.above = StationTileType.ERROR;
            }
            try
            {
                currentNeighborhood.below = data[l - 1, x, y];
            }
            catch
            {
                currentNeighborhood.below = StationTileType.ERROR;
            }
            return currentNeighborhood;
        }

        public StationTileNeighborhood getNeighborsFull(int x, int y, int l)
        {
            currentNeighborhood = new StationTileNeighborhood();

            if (data == null)
            {
                currentNeighborhood.north = StationTileType.ERROR;
                currentNeighborhood.south = StationTileType.ERROR;
                currentNeighborhood.east = StationTileType.ERROR;
                currentNeighborhood.west = StationTileType.ERROR;
                currentNeighborhood.above = StationTileType.ERROR;
                currentNeighborhood.below = StationTileType.ERROR;
                return currentNeighborhood;
            }

            try
            {
                currentNeighborhood.center = data[l, x, y];
            }
            catch
            {
                currentNeighborhood.center = StationTileType.ERROR;
            }

            try
            {
                currentNeighborhood.north = data[l, x, y + 1];
            }
            catch
            {
                currentNeighborhood.north = StationTileType.ERROR;
            }

            try
            {
                currentNeighborhood.south = data[l, x, y - 1];
            }
            catch
            {
                currentNeighborhood.south = StationTileType.ERROR;
            }

            try
            {
                currentNeighborhood.east = data[l, x + 1, y];
            }
            catch
            {
                currentNeighborhood.east = StationTileType.ERROR;
            }

            try
            {
                currentNeighborhood.west = data[l, x - 1, y];
            }
            catch
            {
                currentNeighborhood.west = StationTileType.ERROR;
            }

            try
            {
                currentNeighborhood.above = data[l + 1, x, y];
            }
            catch
            {
                currentNeighborhood.above = StationTileType.ERROR;
            }
            try
            {
                currentNeighborhood.below = data[l - 1, x, y];
            }
            catch
            {
                currentNeighborhood.below = StationTileType.ERROR;
            }
            try
            {
                currentNeighborhood.northeast = data[l, x + 1, y + 1];
            }
            catch
            {
                currentNeighborhood.northeast = StationTileType.ERROR;
            }
            try
            {
                currentNeighborhood.northwest = data[l, x - 1, y + 1];
            }
            catch
            {
                currentNeighborhood.northwest = StationTileType.ERROR;
            }
            try
            {
                currentNeighborhood.southeast = data[l, x + 1, y - 1];
            }
            catch
            {
                currentNeighborhood.southeast = StationTileType.ERROR;
            }
            try
            {
                currentNeighborhood.southwest = data[l, x - 1, y - 1];
            }
            catch
            {
                currentNeighborhood.southwest = StationTileType.ERROR;
            }
            return currentNeighborhood;
        }

        public bool outOfBounds(int x, int y, int l)
        {
            return (x >= width || x < 0 || y >= height || y < 0 || l >= levels || l < 0);
        }
    }
}
