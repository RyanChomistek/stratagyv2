using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class RoadPath
{
    public List<Vector3> Path;
}

[CreateNodeMenu("TileNodes/RoadTileNode")]
public class RoadTileNode : TerrainNode
{
    [Input] public Terrain[] InputTerrain = null;
    [Input] public float[] InputHeightMap = null;
    [Input] public Vector2[] InputGradientMap = null;
    [Input] public Improvement[] InputImprovements = null;

    [Input] public List<TileComponent> LandComponents = null;

    [Input] public int Iterations = 1;

    [Output] public Improvement[] OutputImprovements = null;
    [Output] public List<RoadPath> OutputRoadPaths = null;

    public override object GetValue(XNode.NodePort port)
    {
        if (port.fieldName == "OutputImprovements")
            return OutputImprovements;

        if (port.fieldName == "OutputRoadPaths")
            return OutputRoadPaths;

        return null;
    }

    public override void Recalculate()
    {
        Improvement[] InputImprovements = GetInputValue("InputImprovements", this.InputImprovements);
        Vector2[] InputGradientMap = GetInputValue("InputGradientMap", this.InputGradientMap);
        Terrain[] InputTerrain = GetInputValue("InputTerrain", this.InputTerrain);
        float[] InputHeightMap = GetInputValue("InputHeightMap", this.InputHeightMap);
        List<TileComponent> LandComponents = GetInputValue<List<TileComponent>>("LandComponents", null);
        int Iterations = GetInputValue<int>("Iterations", this.Iterations);
        OutputRoadPaths = new List<RoadPath>();

        if (IsInputArrayValid(InputImprovements) && 
            IsInputArrayValid(InputGradientMap) &&
            IsInputArrayValid(InputTerrain) &&
            IsInputArrayValid(InputHeightMap) &&
            LandComponents != null)
        {
            OutputImprovements = (Improvement[]) InputImprovements.Clone();
            
            for (int i = 0; i < Iterations; i++)
            {
                RoadPath OutputRoadPath = new RoadPath();
                Generate(
                    new SquareArray<Improvement>(OutputImprovements),
                    new SquareArray<Terrain>(InputTerrain),
                    new SquareArray<float>(InputHeightMap),
                    LandComponents,
                    (graph as TerrainGeneratorGraph).Rand,
                    out OutputRoadPath.Path);

                OutputRoadPaths.Add(OutputRoadPath);
            }
        }
    }

    public static void Generate(SquareArray<Improvement> improvements, in SquareArray<Terrain> terrain, SquareArray<float> heightMap, List<TileComponent> landComponents, System.Random rand, out List<Vector3> roadPath)
    {
        bool startFound = FindStartAndDir(rand, landComponents, out Vector2Int start, out Vector2Int end);

        // theres the possibility that theres no land mass big enough to support, in this case abort
        if (!startFound)
        {
            roadPath = null;
            return;
        }

        List<Vector2Int> path = FindPath(improvements, terrain, heightMap, start, end);

        // make sure we actually found a good path
        if (path.Last() == end)
        {
            SetTiles(improvements, path);
        }

        roadPath = path.Select(point =>
            new Vector3(
                point.x,
                heightMap[point.x, point.y],
                point.y)).ToList();
    }

    public static List<Vector2Int> FindPath(SquareArray<Improvement> improvementMap, SquareArray<Terrain> terrainMap, SquareArray<float> heightMap, Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> path = null;
        ProfilingUtilities.LogAction(() =>
        {
            path = CustomAStar.AStar(improvementMap.SideLength, start, end,
                (current, adjacent) =>
                {
                    if (improvementMap[adjacent.x, adjacent.y] == Improvement.Road)
                    {
                        return 0;
                    }

                    if (terrainMap[adjacent.x, adjacent.y] != Terrain.Grass)
                    {
                        return 100;
                    }

                    float currHeight = heightMap[current.x, current.y];
                    float adjHeight = heightMap[adjacent.x, adjacent.y];
                    return Mathf.Abs(currHeight - adjHeight) * 100;
                });
        }, "road path time");
        Debug.Log($"path size {path.Count}");

        return path;
    }

    public static bool FindStartAndDir(
        System.Random rand,
        List<TileComponent> landComponents,
        out Vector2Int start,
        out Vector2Int end)
    {
        //remove all small components
        landComponents = landComponents.Where(x => x.Locations.Count > 200).ToList();

        if (landComponents.Count == 0)
        {
            start = Vector2Int.zero;
            end = Vector2Int.zero;
            Debug.LogError("No componenets found");
            return false;
        }

        // Pick a random component
        TileComponent component = landComponents[rand.Next(landComponents.Count)];

        // Get the edges of the component
        HashSet<Vector2Int> edges = FindEdgesOfComponent(component.Locations);

        // pick a random node on the edge
        start = edges.ElementAt(rand.Next(edges.Count));

        // Find average distance of every other edge to our start
        float sumDistance = 0;
        foreach (var edge in edges)
        {
            sumDistance += (edge - start).magnitude;
        }

        float averageDistance = sumDistance / edges.Count;
        end = start;

        // Pick a random other one thats reasonably far away from the picked one
        while (edges.Count != 0)
        {
            end = edges.ElementAt(rand.Next(edges.Count));
            edges.Remove(end);

            if ((end - start).magnitude > averageDistance)
            {
                break;
            }
        }

        //Debug.Log($"{(end - start).magnitude}, {averageDistance} | start {start} {mapData.TerrainMap[start.x, start.y]}, end {end} {mapData.TerrainMap[end.x, end.y]}");

        return true;
    }

    public static void SetTiles(SquareArray<Improvement> map, List<Vector2Int> path)
    {
        Vector2 dir = Vector2.zero;

        for (int index = 0; index < path.Count; index++)
        {
            Vector2Int pos = path[index];
            map[pos.x, pos.y] = Improvement.Road;

            if (index + 1 < path.Count)
            {
                dir = path[index + 1] - path[index];
            }

            Vector2 tangent = Vector2.Perpendicular(dir).normalized;

            Vector2 realStep = new Vector2(pos.x, pos.y);
            Vector2 delta = pos - realStep;
            while (delta.magnitude < 1)
            {
                Vector2Int stepRounded = VectorUtilityFunctions.RoundVector(realStep);
                if (!map.InBounds(stepRounded))
                {
                    break;
                }

                map[stepRounded.x, stepRounded.y] = Improvement.Road;

                realStep += dir * .1f;
                delta = pos - realStep;
            }
        }
    }

    /// <summary>
    /// finds the edge of a component O(N)
    /// </summary>
    /// <param name="component"></param>
    /// <returns></returns>
    public static HashSet<Vector2Int> FindEdgesOfComponent(HashSet<Vector2Int> component)
    {
        HashSet<Vector2Int> edges = new HashSet<Vector2Int>();

        Vector2Int[] adjacentDirections = {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right,
        };

        foreach (var node in component)
        {
            foreach (Vector2Int dir in adjacentDirections)
            {
                if (!component.Contains(node + dir))
                {
                    edges.Add(node);
                }
            }
        }

        return edges;
    }

    public override void Flush()
    {
        InputTerrain = null;
        InputHeightMap = null;
        InputGradientMap = null;
        InputImprovements = null;
        LandComponents = null;
        OutputImprovements = null;
        OutputRoadPaths = null;
    }
}
