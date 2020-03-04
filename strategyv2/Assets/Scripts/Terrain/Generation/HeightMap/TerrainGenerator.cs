using System.Collections.Generic;
using UnityEngine;

namespace HeightMapGeneration
{
    public class TerrainGenerator : MonoBehaviour 
    {
        // Include padding because some operations don't function well close to the border of the map
        public int MapPadding = 3;

        HeightMapGenerationData HMData;

        public List<GenerationNode> GenerationNodes = new List<GenerationNode>();

        public void GenerateHeightMap(int mapSize) {

            //HeightMap = FindObjectOfType<HeightMapGenerator>().GenerateHeightMap(mapSizeWithBorder);

            //LakeMap = new float[mapSizeWithBorder * mapSizeWithBorder];
            int mapSizeWithBorder = mapSize + MapPadding * 2;
            HMData = new HeightMapGenerationData(mapSizeWithBorder);

            foreach (var node in GenerationNodes)
            {
                if(!node.Enabled)
                {
                    continue;
                }

                HeightMapLayerBase layer = null;
                switch (node.Type)
                {
                    case Algorithm.RandomNoise:
                        //layer = new RandomNoiseHeightMapLayer(node.GetSettings() as RandomNoiseLayerSettings);
                        break;
                    case Algorithm.Erosion:
                        layer = new ErosionHeightMapLayer(node.GetSettings() as ErosionLayerSettings, MapPadding);
                        break;
                    case Algorithm.Terrace:
                        layer = new TerraceHeightMapLayer(node.GetSettings() as TerraceLayerSettings);
                        break;
                    default:
                        Debug.LogError("missing algorithm please add");
                        continue;
                }

                layer.Apply(HMData);
            }
        }

        public void ConvertMapsToTileMaps(MapData mapdata)
        {
            mapdata.VertexHeightMap = new float[mapdata.MeshHeightMapSize, mapdata.MeshHeightMapSize];
            mapdata.RawWaterLevelMap = new float[mapdata.MeshHeightMapSize, mapdata.MeshHeightMapSize];

            for (int x = MapPadding; x < HMData.MapSize - MapPadding; x++)
            {
                for (int y = MapPadding; y < HMData.MapSize - MapPadding; y++)
                {
                    mapdata.VertexHeightMap[x - MapPadding, y - MapPadding] = HMData.HeightMap[x, y];
                    mapdata.RawWaterLevelMap[x - MapPadding, y - MapPadding] = HMData.WaterMap[x, y];
                }
            }

            //LayerMapFunctions.SmoothMT(ref mapdata.VertexHeightMap, 2);

            // Create the tile height map
            // take every 4 height map points and find the middle value and use that
            for (int x = 0; x < mapdata.MeshHeightMapSize - 1; x++)
            {
                for (int y = 0; y < mapdata.MeshHeightMapSize - 1; y++)
                {
                    Vector2Int rawIndex = new Vector2Int(x, y);
                    Vector2Int[] indexes = new Vector2Int[]
                    {
                        new Vector2Int(rawIndex.x,     rawIndex.y + 1), // topLeft
                        new Vector2Int(rawIndex.x + 1, rawIndex.y + 1), // topRight
                        new Vector2Int(rawIndex.x + 1, rawIndex.y),     // bottomRight
                        new Vector2Int(rawIndex.x,     rawIndex.y),     // bottomLeft
                    };

                    float[] heights = new float[4];
                    for (int i = 0; i < indexes.Length; i++)
                    {
                        heights[i] = mapdata.VertexHeightMap[indexes[i].x, indexes[i].y];
                    }

                    float[] waterLevels = new float[4];
                    for (int i = 0; i < indexes.Length; i++)
                    {
                        waterLevels[i] = mapdata.RawWaterLevelMap[indexes[i].x, indexes[i].y];
                    }

                    Vector2 uv = new Vector2(.5f, .5f);

                    float waterLevel = TerrainMeshGenerator.QuadLerp(waterLevels[0], waterLevels[1], waterLevels[2], waterLevels[3], uv.x, uv.y);
                    mapdata.WaterMap[x, y] = waterLevel;

                    float height = TerrainMeshGenerator.QuadLerp(heights[0], heights[1], heights[2], heights[3], uv.x, uv.y);
                    mapdata.HeightMap[x, y] = height;
                }
            }
        }

        public static float[] Convert2DMapTo1D(int mapSize, float[,] map)
        {
            float[] flatMap = new float[mapSize * mapSize];

            for (int x = 0; x <= map.GetUpperBound(0); x++)
            {
                for (int y = 0; y <= map.GetUpperBound(1); y++)
                {
                    int i = (y * mapSize) + x;
                    flatMap[i] = map[x, y];
                }
            }

            return flatMap;
        }

        public static float[,] Convert1DMapTo2D(int mapSize, float[] map)
        {
            float[,] twoDMap = new float[mapSize, mapSize];

            for (int i = 0; i < mapSize * mapSize; i++)
            {
                int x = i % mapSize;
                int y = i / mapSize;
                twoDMap[x, y] = map[i];
            }

            return twoDMap;
        }
    }
}