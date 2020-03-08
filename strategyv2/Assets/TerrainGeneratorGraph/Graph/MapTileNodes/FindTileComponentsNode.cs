using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TileComponent
{
    public HashSet<Vector2Int> Locations;

    public TileComponent()
    {
        Locations = new HashSet<Vector2Int>();
    }

    public void Add(Vector2Int location) { Locations.Add(location); }
}


[CreateNodeMenu("TileNodes/FindTileComponentsNode")]
public class FindTileComponentsNode : TerrainNode
{
    [Input] public Terrain[] InputTerrain;
    [Input] public Terrain TypeOfComponent;

    [Output] public List<TileComponent> OutputComponents;
    [Output] public int[] OutputComponenetsMap;

    public override object GetValue(XNode.NodePort port)
    {
        if (port.fieldName == "OutputComponents")
            return OutputComponents;

        if (port.fieldName == "OutputComponenetsMap")
            return OutputComponenetsMap;

        return null;
    }

    public override void Recalculate()
    {
        Terrain[] InputTerrain = GetInputValue("InputTerrain", this.InputTerrain);
        Terrain Terrain = GetInputValue("TypeOfComponent", this.TypeOfComponent);

        if(IsInputArrayValid(InputTerrain))
        {
            SquareArray<Terrain> InputTerrainSquare = new SquareArray<Terrain>(InputTerrain);
            List<TileComponent> componentsList;
            SquareArray<int> componentMap;
            FindComponents(Terrain, 0, InputTerrainSquare, out componentsList, out componentMap);

            OutputComponents = componentsList;
            OutputComponenetsMap = componentMap.Array;
        }
    }

    public static List<TileComponent> FindComponents(Terrain terrain, int bufferWidth, SquareArray<Terrain> terrainTileMap, out List<TileComponent> components, out SquareArray<int> componentMap)
    {
        componentMap = new SquareArray<int>(terrainTileMap.SideLength);

        int componentCounter = 1;

        // MUST NOT USE DIAGONALS will mess up mesh generation
        Vector2Int[] floodFillDirections = {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right,
        };

        components = new List<TileComponent>();

        // 0 represents unmarked, -1 is a failure, and any other number is a component
        for (int y = 0; y < componentMap.SideLength; y++)
        {
            for (int x = 0; x < componentMap.SideLength; x++)
            {
                if (terrainTileMap[x, y] == terrain && componentMap[x, y] == 0)
                {
                    TileComponent componet = FloodFill(
                        terrain,
                        componentMap,
                        new Vector2Int(x, y),
                        componentCounter,
                        floodFillDirections,
                        bufferWidth,
                        terrainTileMap);

                    components.Add(componet);
                    componentCounter++;
                }

            }
        }

        return components;
    }

    private static TileComponent FloodFill(
        Terrain terrain,
        SquareArray<int> componentMap,
        Vector2Int startPos,
        int componentNumber,
        Vector2Int[] floodFillDirections,
        int bufferWidth,
        SquareArray<Terrain> terrainTileMap)
    {
        TileComponent componet = new TileComponent();

        Stack<Vector2Int> cellsToBeProcessed = new Stack<Vector2Int>();
        cellsToBeProcessed.Push(startPos);

        List<Vector2Int> adjacentDirections = new List<Vector2Int>();

        if (bufferWidth > 0)
        {
            adjacentDirections = new List<Vector2Int>() {
                    Vector2Int.zero,
                    Vector2Int.up,
                    Vector2Int.down,
                    Vector2Int.left,
                    Vector2Int.right,
                    Vector2Int.up + Vector2Int.left,
                    Vector2Int.up + Vector2Int.right,
                    Vector2Int.down + Vector2Int.left,
                    Vector2Int.down + Vector2Int.right,
                };

            for (int i = 0; i < bufferWidth - 1; i++)
            {
                List<Vector2Int> temp_adjacentDirections = new List<Vector2Int>();
                foreach (var dir in adjacentDirections)
                {
                    temp_adjacentDirections.Add(dir);
                    foreach (var dir2 in adjacentDirections)
                    {
                        temp_adjacentDirections.Add(dir + dir2);
                    }
                }
            }
        }

        while (cellsToBeProcessed.Count > 0)
        {
            Vector2Int currPos = cellsToBeProcessed.Pop();
            componet.Add(currPos);
            componentMap[currPos.x, currPos.y] = componentNumber;

            foreach (var dir in floodFillDirections)
            {
                Vector2Int newPos = currPos + dir;
                if (IsUnmarkedTile(terrain, newPos, adjacentDirections, componentMap, terrainTileMap))
                {
                    cellsToBeProcessed.Push(newPos);
                }
            }
        }

        return componet;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="terrain"></param>
    /// <param name="Pos"></param>
    /// <param name="bufferWidth"> if this is greater than 0 a buffer zone around the components will be included </param>
    /// <param name="componentMap"></param>
    /// <param name="terrainTileMap"></param>
    /// <returns></returns>
    private static bool IsUnmarkedTile(
        Terrain terrain,
        Vector2Int Pos,
        List<Vector2Int> adjacentDirections,
        SquareArray<int> componentMap,
        SquareArray<Terrain> terrainTileMap)
    {
        bool inBounds = componentMap.InBounds(Pos);
        if (inBounds && componentMap[Pos.x, Pos.y] == 0)
        {
            bool isCorrectTileType = terrainTileMap[Pos.x, Pos.y] == terrain;

            if (isCorrectTileType)
            {
                return true;
            }
            else
            {
                // check adjacent tiles
                foreach (var adj in adjacentDirections)
                {
                    Vector2Int adjacentPos = new Vector2Int(Pos.x + adj.x, Pos.y + adj.y);
                    if (componentMap.InBounds(adjacentPos) && terrainTileMap[adjacentPos.x, adjacentPos.y] == terrain)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public override void Flush()
    {
        InputTerrain = null;
        OutputComponenetsMap = null;
    }
}
