using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
class Location
{
    public int Col;
    public int Row;
    public float CombinedCost; // F
    public float CostFromStart; // G
    public float CostToGoal; // H
    public Location Parent;
    public Vector2Int Position {get { return new Vector2Int(Row, Col); } }

    public override bool Equals(object obj)
    {
        return obj is Location location &&
               Col == location.Col &&
               Row == location.Row;
    }

    public override int GetHashCode()
    {
        var hashCode = 973646036;
        hashCode = hashCode * -1521134295 + Col.GetHashCode();
        hashCode = hashCode * -1521134295 + Row.GetHashCode();
        return hashCode;
    }
}

class CustomAStar
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="mapData"></param>
    /// <param name="startPos"></param>
    /// <param name="targetPos"></param>
    /// <param name="adjacentCostFunction"> calculates the cost between two nodes</param>
    /// <returns></returns>
    public static List<Vector2Int> AStar(MapData mapData, Vector2Int startPos, Vector2Int targetPos, Func<Vector2Int, Vector2Int, float> adjacentCostFunction)
    {
        Location current = null;

        var start = new Location { Row = startPos.x, Col = startPos.y };
        var target = new Location { Row = targetPos.x, Col = targetPos.y };

        var openList = new HashSet<Location>();
        var closedList = new HashSet<Location>();

        // start by adding the original position to the open list
        openList.Add(start);
        int cnt = 0;

        System.TimeSpan totalTime;

        System.TimeSpan getAdjacentSquaresTime;
        System.TimeSpan isDestinationFoundTime;
        System.TimeSpan handleAdjacentSquaresTime;

        float gradientSum = 0;
        float gradientMin = float.MaxValue;
        float gradientMax = float.MinValue;
        int gradientCnt = 0;
        foreach (var elem in mapData.GradientMap)
        {
            gradientSum += elem.magnitude;
            gradientMin = Mathf.Min(gradientMin, elem.magnitude);
            gradientMax = Mathf.Max(gradientMax, elem.magnitude);
            gradientCnt += 1;
        }

        float gradientAverage = gradientSum / gradientCnt;

        Debug.Log($"average gradient {gradientAverage}, min {gradientMin}, max {gradientMax}");

        LayerMapFunctions.LogActionAggrigate(() => { 
            while (openList.Count > 0 && cnt < 50000)
            {
                cnt++;
                float lowest = -1;
                // get the square with the lowest CombinedCost
                lowest = openList.Min(l => l.CombinedCost);
                current = openList.First(l => l.CombinedCost == lowest);
            
                // add the current square to the closed list
                closedList.Add(current);

                // remove it from the open list
                openList.Remove(current);

                bool isDestinationFound = false;

                LayerMapFunctions.LogActionAggrigate(() => { isDestinationFound = closedList.FirstOrDefault(l => l.Col == target.Col && l.Row == target.Row) != null; }, ref getAdjacentSquaresTime);

                // if we added the destination to the closed list, we've found a path
                if (isDestinationFound)
                    break;

                Location[] adjacentSquares = null;
                LayerMapFunctions.LogActionAggrigate(() => { adjacentSquares = GetWalkableAdjacentSquares(current.Col, current.Row, mapData, gradientAverage, gradientMin, gradientMax); }, ref getAdjacentSquaresTime);
                
                LayerMapFunctions.LogActionAggrigate(() =>
                {
                    foreach (var adjacent in adjacentSquares)
                    {
                        // if this adjacent square is already in the closed list, ignore it
                        if (adjacent == null || closedList.Contains(adjacent))
                            continue;

                        float CostFromStart_temp = current.CostFromStart + ComputeCostFromCurrentToAdjacent(current, adjacent, mapData, adjacentCostFunction);

                        // if it's not in the open list...
                        if (!openList.Contains(adjacent))
                        {
                            // compute its score, set the parent
                            adjacent.CostFromStart = CostFromStart_temp;
                            adjacent.CostToGoal = ComputeCostToTarget(adjacent.Col, adjacent.Row, target.Col, target.Row);
                            adjacent.CombinedCost = adjacent.CostFromStart + adjacent.CostToGoal;
                            adjacent.Parent = current;

                            // and add it to the open list
                            openList.Add(adjacent);
                        }
                        else
                        {
                            // test if using the current G score makes the adjacent square's F score
                            // lower, if yes update the parent because it means it's a better path
                            if (CostFromStart_temp + adjacent.CostToGoal < adjacent.CombinedCost)
                            {
                                adjacent.CostFromStart = CostFromStart_temp;
                                adjacent.CombinedCost = adjacent.CostFromStart + adjacent.CostToGoal;
                                adjacent.Parent = current;
                            }
                        }
                    }
                }, ref handleAdjacentSquaresTime);
            }
        }, ref totalTime);

        List<Vector2Int> path = new List<Vector2Int>();

        // assume path was found; let's show it
        while (current != null)
        {
            path.Add(current.Position);
            //Debug.Log(current.Position);
            current = current.Parent;
        }

        path.Reverse();

        //Debug.Log($"cnt {cnt}");
        //Debug.Log($"time total  : {totalTime}");
        //Debug.Log($"time adjacentSquares  : {getAdjacentSquaresTime} {getAdjacentSquaresTime.Ticks / (double)totalTime.Ticks}");
        //Debug.Log($"time handleAdjacentSquares  : {handleAdjacentSquaresTime} {handleAdjacentSquaresTime.Ticks / (double)totalTime.Ticks}");
        //Debug.Log($"time destination found: {isDestinationFoundTime} {isDestinationFoundTime.Ticks / (double)totalTime.Ticks}");

        //PrintSolution(mapData, path, start, target, current);

        return path;
    }

    static Location[] GetWalkableAdjacentSquares(int col, int row, MapData mapData, float averageGradient, float gradientMin, float gradientMax)
    {
        float heightOriginal = mapData.HeightMap[row, col];
        var proposedLocations = new Location[]
        {
            new Location { Col = col, Row = row - 1 },
            new Location { Col = col, Row = row + 1 },
            new Location { Col = col - 1, Row = row },
            new Location { Col = col + 1, Row = row },
            new Location { Col = col + 1, Row = row + 1 },
            new Location { Col = col + 1, Row = row - 1 },
            new Location { Col = col - 1, Row = row + 1 },
            new Location { Col = col - 1, Row = row - 1 },
        };

        for (int i = 0; i < proposedLocations.Length; i++)
        {
            Location l = proposedLocations[i];
            if(!LayerMapFunctions.InBounds(mapData.TerrainMap, l.Position))
            {
                proposedLocations[i] = null;
                continue;
            }

            if (mapData.TerrainMap[l.Row, l.Col] != Terrain.Grass)
            {
                proposedLocations[i] = null;
            }
        }

        return proposedLocations;
    }

    static int ComputeCostToTarget(int x, int y, int targetX, int targetY)
    {
        return Math.Abs(targetX - x) + Math.Abs(targetY - y);
    }

    static float ComputeCostFromCurrentToAdjacent(Location current, Location adjacent, MapData mapData, Func<Vector2Int, Vector2Int, float> adjacentCostFunction)
    {
        float currHeight = mapData.HeightMap[current.Row, current.Col];
        float adjHeight = mapData.HeightMap[adjacent.Row, adjacent.Col];
        //+ 
        return 1 + adjacentCostFunction(current.Position, adjacent.Position);
    }

    static void PrintSolution(MapData mapData, List<Vector2Int> path, Location start, Location target, Location current)
    {
        string str = "";
        for (int row = mapData.TerrainMap.GetUpperBound(0); row >= 0; row--)
        {
            str += row + ": ";

            for (int col = 0; col <= mapData.TerrainMap.GetUpperBound(1); col++)
            {
                var pos = new Vector2Int(row, col);
                if (pos == start.Position)
                {
                    str += "A    ";
                }
                else if (pos == target.Position)
                {
                    str += "Z    ";
                }
                else if (path.Contains(pos))
                {
                    str += "X    ";
                }
                else
                {
                    str += mapData.TerrainMap[row, col];
                }

                str += ", ";
            }

            str += "\n";
        }

        //Debug.Log(str);

        var sr = File.CreateText($"Assets/Saves/roadPathSolution.txt");
        sr.WriteLine(str);
        sr.Close();
    }
}

