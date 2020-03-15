using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathCreation;

public class MeshGenerationNode : TerrainNode
{
    [Input] public float[] TileHeightMap = null;
    [Input] public float[] VertexHeightMap = null;
    [Input] public float[] TileWaterMap = null;
    [Input] public float[] VertexWaterMap = null;
    [Input] public Terrain[] TerrainMap = null;
    [Input] public Improvement[] ImprovementMap = null;
    [Input] public Vector2[] GradientTileMap = null;
    [Input] public Vector2[] LayeredGradientMap = null;
    [Input] public List<RoadPath> RoadPaths = null;
    public override object GetValue(XNode.NodePort port)
    {
        Debug.LogError("MeshGenerationNode should not be used for output");
        return null;
    }

    public override void Recalculate()
    {
        TileHeightMap = GetInputValue("TileHeightMap", this.TileHeightMap);
        VertexHeightMap = GetInputValue("VertexHeightMap", this.VertexHeightMap);
        TileWaterMap = GetInputValue("TileWaterMap", this.TileWaterMap);
        VertexWaterMap = GetInputValue("VertexWaterMap", this.VertexWaterMap);
        TerrainMap = GetInputValue("TerrainMap", this.TerrainMap);
        ImprovementMap = GetInputValue("ImprovementMap", this.ImprovementMap);
        GradientTileMap = GetInputValue("GradientTileMap", this.GradientTileMap);
        LayeredGradientMap = GetInputValue("LayeredGradientMap", this.LayeredGradientMap);
        LayeredGradientMap = GetInputValue("LayeredGradientMap", this.LayeredGradientMap);
        RoadPaths = GetInputValue("RoadPaths", this.RoadPaths);
    }

    public MapData GetValueAsMapData()
    {
        MapData mapData = new MapData();

        mapData.TerrainMap = new SquareArray<Terrain>(TerrainMap);
        mapData.ImprovmentMap = new SquareArray<Improvement>(ImprovementMap);
        if(TileHeightMap != null)
            mapData.HeightMap = new SquareArray<float>(TileHeightMap);
        mapData.VertexHeightMap = new SquareArray<float>(VertexHeightMap);
        mapData.WaterMap = new SquareArray<float>(TileWaterMap);
        mapData.VertexWaterLevelMap = new SquareArray<float>(VertexWaterMap);
        mapData.GradientMap = new SquareArray<Vector2>(GradientTileMap);
        if(LayeredGradientMap != null)
            mapData.LayeredGradientMap = new SquareArray<Vector2>(LayeredGradientMap);

        if(RoadPaths != null)
        {
            List<List<Vector3>> outputRoadPaths = new List<List<Vector3>>();
            RoadPaths.ForEach(x => outputRoadPaths.Add(x.Path));
            mapData.RoadPaths = outputRoadPaths;
        }
        

        return mapData;
    }

    public override void Flush()
    {
        TileHeightMap = null;
        VertexHeightMap = null;
        TileWaterMap = null;
        VertexWaterMap = null;
        TerrainMap = null;
        ImprovementMap = null;
        GradientTileMap = null;
        LayeredGradientMap = null;
        RoadPaths = null;
    }
}
