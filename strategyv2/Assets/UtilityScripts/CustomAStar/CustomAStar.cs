using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
class Location : Priority_Queue.GenericPriorityQueueNode<float>
{
    public int Col;
    public int Row;
    public float CombinedCost; // F
    public float CostFromStart; // G
    public float CostToGoal; // H
    public Location Parent;
    public Vector2Int Position {get { return new Vector2Int(Row, Col); } }

    public Location()
    {
        CombinedCost = 0;
    }

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

    public override string ToString()
    {
        return $"{Row}, {Col}";
    }

    public static bool operator ==(Location a, Location b)
    {
        bool IsANull = (object)a == null;
        bool IsBNull = (object)b == null;

        if (IsANull || IsBNull)
            return IsANull == IsBNull;

        return a.Equals(b);
    }

    public static bool operator !=(Location a, Location b)
    {
        return !(a == b);
    }
}

public class CustomAStar
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="mapData"></param>
    /// <param name="startPos"></param>
    /// <param name="targetPos"></param>
    /// <param name="adjacentCostFunction"> calculates the cost between two nodes</param>
    /// <returns></returns>
    public static List<Vector2Int> AStar(int mapSize, Vector2Int startPos, Vector2Int targetPos, Func<Vector2Int, Vector2Int, float> adjacentCostFunction)
    {
        float maxCost = (targetPos - startPos).magnitude * 2;
        Location current = null;

        var start = new Location { Row = startPos.x, Col = startPos.y };
        var target = new Location { Row = targetPos.x, Col = targetPos.y };

        Dictionary<int, Location> insertedNodeStorage = new Dictionary<int, Location>();
        Priority_Queue.GenericPriorityQueue<Location, float> openList = new Priority_Queue.GenericPriorityQueue<Location, float>(mapSize * mapSize);

        // start by adding the original position to the open list
        insertedNodeStorage.Add(start.GetHashCode(), start);
        openList.Enqueue(start, 0);
        int cnt = 0;

        System.TimeSpan totalTime;
        System.TimeSpan getAdjacentSquaresTime;
        System.TimeSpan handleAdjacentSquaresTime;
        System.TimeSpan dequeueTime;

        ProfilingUtilities.LogActionAggrigate(() => {
            while (openList.Count > 0 && cnt < 20000)
            {
                cnt++;
                // Get the minimum priority item
                ProfilingUtilities.LogActionAggrigate(() =>
                {
                    current = openList.Dequeue();
                }, ref dequeueTime);

                // if we added the destination to the closed list, we've found a path
                if (current.Equals(target))
                {
                    Debug.Log($"Astar exited normaly after {cnt} iterations");
                    break;
                }
                
                Location[] adjacentSquares = null;
                ProfilingUtilities.LogActionAggrigate(() => { adjacentSquares = GetWalkableAdjacentSquares(current.Col, current.Row, mapSize, insertedNodeStorage); }, ref getAdjacentSquaresTime);

                ProfilingUtilities.LogActionAggrigate(() =>
                {
                    HandleNeighbors(current, target, adjacentSquares, openList, insertedNodeStorage, adjacentCostFunction);
                }, ref handleAdjacentSquaresTime);
            
            }
        }, ref totalTime);

        List<Vector2Int> path = new List<Vector2Int>();

        Debug.Log($"end {current} | target {target}");

        // assume path was found; let's show it
        while (current != null)
        {
            path.Add(current.Position);
            current = current.Parent;
        }

        path.Reverse();
        
        Debug.Log($"astar iterations {cnt}");
        Debug.Log($"astar time total  : {totalTime}");
        Debug.Log($"astar time dequeue  : {dequeueTime} {dequeueTime.Ticks / (double)totalTime.Ticks}");
        Debug.Log($"astar time adjacentSquares  : {getAdjacentSquaresTime} {getAdjacentSquaresTime.Ticks / (double)totalTime.Ticks}");
        Debug.Log($"astar time handleAdjacentSquares  : {handleAdjacentSquaresTime} {handleAdjacentSquaresTime.Ticks / (double)totalTime.Ticks}");
        //Debug.Log($"time destination found: {isDestinationFoundTime} {isDestinationFoundTime.Ticks / (double)totalTime.Ticks}");

        //PrintSolution(mapData, path, start, target, current);

        return path;
    }

    private static void HandleNeighbors(Location current,
        Location target,
        Location[] adjacentSquares,
        Priority_Queue.GenericPriorityQueue<Location, float> openList,
        Dictionary<int, Location> insertedNodeStorage,
        Func<Vector2Int, Vector2Int, float> adjacentCostFunction)
    {
        foreach (var adjacent in adjacentSquares)
        {
            // if this adjacent square is already in the closed list, ignore it
            //if (adjacent == null || closedList.Contains(adjacent))
            if (adjacent == null)
                continue;

            float CostFromStart_temp = current.CostFromStart + ComputeCostFromCurrentToAdjacent(current, adjacent, adjacentCostFunction);

            if (CostFromStart_temp < adjacent.CostFromStart)
            {
                // if it's not in the open list...
                //if (!openListFastStorage.ContainsKey(adjacent.GetHashCode()))
                if (!openList.Contains(adjacent))
                {
                    // compute its score, set the parent
                    adjacent.CostFromStart = CostFromStart_temp;
                    adjacent.CostToGoal = ComputeCostToTarget(adjacent.Col, adjacent.Row, target.Col, target.Row);
                    adjacent.CombinedCost = adjacent.CostFromStart + adjacent.CostToGoal;
                    adjacent.Parent = current;

                    // and add it to the open list
                    openList.Enqueue(adjacent, adjacent.CombinedCost);

                    insertedNodeStorage[adjacent.GetHashCode()] = adjacent;
                }
                else
                {
                    // test if using the current G score makes the adjacent square's F score
                    // lower, if yes update the parent because it means it's a better path
                    //if (CostFromStart_temp + adjacent.CostToGoal < adjacent.CombinedCost)
                    {
                        adjacent.CostFromStart = CostFromStart_temp;
                        adjacent.CombinedCost = adjacent.CostFromStart + adjacent.CostToGoal;
                        adjacent.Parent = current;
                        openList.UpdatePriority(adjacent, adjacent.CombinedCost);
                        insertedNodeStorage[adjacent.GetHashCode()] = adjacent;
                    }
                }
            }
        }
    }


    public static bool InBounds(int mapSize, int x, int y)
    {
        if (x < mapSize &&
            y < mapSize &&
            x >= 0 &&
            y >= 0)
        {
            return true;
        }

        return false;
    }

    static Location[] GetWalkableAdjacentSquares(int col, int row, int mapSize, Dictionary<int, Location> insertedNodeStorage)
    {
        var proposedLocations = new Location[]
        {
            new Location { Col = col, Row = row - 1, CostFromStart = Mathf.Infinity },
            new Location { Col = col, Row = row + 1, CostFromStart = Mathf.Infinity },
            new Location { Col = col - 1, Row = row, CostFromStart = Mathf.Infinity },
            new Location { Col = col + 1, Row = row, CostFromStart = Mathf.Infinity },
            new Location { Col = col + 1, Row = row + 1, CostFromStart = Mathf.Infinity },
            new Location { Col = col + 1, Row = row - 1, CostFromStart = Mathf.Infinity },
            new Location { Col = col - 1, Row = row + 1, CostFromStart = Mathf.Infinity },
            new Location { Col = col - 1, Row = row - 1, CostFromStart = Mathf.Infinity },
        };

        for (int i = 0; i < proposedLocations.Length; i++)
        {
            if(insertedNodeStorage.TryGetValue(proposedLocations[i].GetHashCode(), out Location loc))
            {
                proposedLocations[i] = loc;
            }

            Location l = proposedLocations[i];
            if(!InBounds(mapSize, l.Position.x, l.Position.y))
            {
                proposedLocations[i] = null;
                continue;
            }
        }

        return proposedLocations;
    }

    static int ComputeCostToTarget(int x, int y, int targetX, int targetY)
    {
        //return Math.Abs(targetX - x) + Math.Abs(targetY - y);
        int dx = targetX - x;
        int dy = targetY - y;
        return Mathf.FloorToInt(Mathf.Sqrt(dx * dx + dy * dy));
    }

    static float ComputeCostFromCurrentToAdjacent(Location current, Location adjacent, Func<Vector2Int, Vector2Int, float> adjacentCostFunction)
    {
        return 1 + adjacentCostFunction(current.Position, adjacent.Position);
    }

    //static void PrintSolution(MapData mapData, List<Vector2Int> path, Location start, Location target, Location current)
    //{
    //    string str = "";
    //    for (int row = mapData.TerrainMap.GetUpperBound(0); row >= 0; row--)
    //    {
    //        str += row + ": ";

    //        for (int col = 0; col <= mapData.TerrainMap.GetUpperBound(1); col++)
    //        {
    //            var pos = new Vector2Int(row, col);
    //            if (pos == start.Position)
    //            {
    //                str += "A    ";
    //            }
    //            else if (pos == target.Position)
    //            {
    //                str += "Z    ";
    //            }
    //            else if (path.Contains(pos))
    //            {
    //                str += "X    ";
    //            }
    //            else
    //            {
    //                str += mapData.TerrainMap[row, col];
    //            }

    //            str += ", ";
    //        }

    //        str += "\n";
    //    }

    //    //Debug.Log(str);

    //    var sr = File.CreateText($"Assets/Saves/roadPathSolution.txt");
    //    sr.WriteLine(str);
    //    sr.Close();
    //}
}

