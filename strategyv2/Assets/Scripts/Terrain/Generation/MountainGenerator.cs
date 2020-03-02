using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MountainGenerator : MonoBehaviour
{
    public static void Generate(MapData mapdata, MapLayerSettings layerSetting)
    {
        for (int x = 0; x <= mapdata.TerrainMap.GetUpperBound(0); x++)
        {
            for (int y = 0; y <= mapdata.TerrainMap.GetUpperBound(1); y++)
            {
                float height = mapdata.HeightMap[x, y];
                float gradient = mapdata.GradientMap[x, y].magnitude;

                bool inHeightRange = height > layerSetting.MinHeight && height <= layerSetting.MaxHeight;
                bool inGradientRange = gradient > layerSetting.MinGradient && gradient <= layerSetting.MaxGradient;

                if (mapdata.TerrainMap[x, y] == Terrain.Grass && (inHeightRange || inGradientRange))
                {
                    mapdata.TerrainMap[x,y] = Terrain.Mountain;
                }
            }
        }
    }
}
