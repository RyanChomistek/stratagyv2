using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HeightMapGeneration
{
    public class TerraceHeightMapLayer : HeightMapLayerBase
    {
        TerraceLayerSettings Settings;

        public TerraceHeightMapLayer(TerraceLayerSettings settings)
        {
            Settings = settings;
        }

        public void Apply(HeightMapGenerationData HMData)
        {
            TerraceMap(Settings.NumLayers, HMData.HeightMap);
        }

        private int GetLayerIndexByHeight(float height, int numLayers)
        {
            int z = (int)(height * numLayers);
            //if the z is exactly numzlayers it will cause at out of bound on out bounds on the layers list
            if (z == numLayers)
            {
                z--;
            }
            return z;
        }

        private void TerraceMap(int numLayers, float[,] map)
        {
            for (int x = 0; x <= map.GetUpperBound(0); x++)
            {
                for (int y = 0; y <= map.GetUpperBound(1); y++)
                {
                    map[x, y] = GetLayerIndexByHeight(map[x, y], numLayers) / (float)numLayers;
                }
            }
        }
    }
}
