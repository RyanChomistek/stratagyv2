using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    struct VertOutsideEdgeInfo
    {
        public Vector3 vert;
        public bool IsOutsideVert;
    }


    public class EditModeTestScript
    {
        // A Test behaves as an ordinary method
        [Test]
        public void TestFindEdgeLoop2()
        {
            List<Vector3> verts = new List<Vector3>();
            List<int> tri = new List<int>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            HashSet<Edge> Edges = new HashSet<Edge>();
            Dictionary<Vector3, int> vertPositionMap = new Dictionary<Vector3, int>();
            Dictionary<Vector3, List<Edge>> edgesIntoPoint = new Dictionary<Vector3, List<Edge>>();
            List<List<Vector3>> OrderedEdgeVerts = new List<List<Vector3>>();

            List<Rect> rects = new List<Rect>();

            rects.Add(new Rect(new Vector2(61.5f, 29.5f), new Vector2(6, 1)));
            rects.Add(new Rect(new Vector2(58.5f, 27.5f), new Vector2(5, 5)));
            rects.Add(new Rect(new Vector2(64.5f, 27.5f), new Vector2(1, 5)));

            ZoneDisplay.CreateMeshesHelper(rects, ref verts, ref vertPositionMap, ref Edges, ref edgesIntoPoint, ref OrderedEdgeVerts, ref tri, ref normals, ref uv);

        }

        [Test]
        public void TestFindEdgeLoop()
        {
            List<Vector3> verts = new List<Vector3>();
            List<int> tri = new List<int>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            HashSet<Edge> Edges = new HashSet<Edge>();
            Dictionary<Vector3, int> vertPositionMap = new Dictionary<Vector3, int>();
            Dictionary<Vector3, List<Edge>> edgesIntoPoint = new Dictionary<Vector3, List<Edge>>();

            List<Rect> rects = new List<Rect>();

            rects.Add(new Rect(new Vector2(61.5f, 29.5f), new Vector2(6, 1)));
            rects.Add(new Rect(new Vector2(58.5f,27.5f), new Vector2(5, 5)));
            rects.Add(new Rect(new Vector2(64.5f, 27.5f), new Vector2(1, 5)));

            foreach (Rect boundingBox in rects)
            {
                Rect rect = Rect.MinMaxRect(boundingBox.xMin, boundingBox.yMin, boundingBox.xMax, boundingBox.yMax);
                ZoneDisplay.CreateFillQuad(rect, ref vertPositionMap, ref verts, ref Edges, ref edgesIntoPoint, ref tri, ref normals, ref uv);
            }


            //find verts on the outside edge
            var sortedVerts = new List<VertOutsideEdgeInfo>();
            foreach (var vert in verts)
            {
                sortedVerts.Add(new VertOutsideEdgeInfo() { vert = vert });
            }

            //sort by x then y
            sortedVerts.Sort((a, b) => {
                var x = a.vert.x.CompareTo(b.vert.x);
                if (x == 0)
                {
                    var y = a.vert.y.CompareTo(b.vert.y);
                    return y;
                }

                return x;
            });

            //look at every vert, if it is missing any edges 
            HashSet<Vector3> edgeVertsSet = new HashSet<Vector3>();
            for (int i = 0; i < sortedVerts.Count; i++)
            {
                VertOutsideEdgeInfo vertInfo = sortedVerts[i];
                Vector3 position = vertInfo.vert;

                vertInfo.IsOutsideVert = false;
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        if (x != 0 && y != 0)
                        {
                            Vector3 nextPoint = position + new Vector3(x, y);
                            if (!Edges.Contains(new Edge(position, nextPoint)))
                            {
                                vertInfo.IsOutsideVert = true;
                                continue;
                            }
                        }
                    }
                }

                if (vertInfo.IsOutsideVert)
                {
                    edgeVertsSet.Add(position);
                }

                /*
                bool up = vertPositionMap.ContainsKey(position + Vector3.up);
                bool right = vertPositionMap.ContainsKey(position + Vector3.right);
                bool down = vertPositionMap.ContainsKey(position + Vector3.down);
                bool left = vertPositionMap.ContainsKey(position + Vector3.left);

                bool upRight = vertPositionMap.ContainsKey(position + new Vector3(1,1));
                bool downright = vertPositionMap.ContainsKey(position + new Vector3(-1, 1));
                bool upLeft = vertPositionMap.ContainsKey(position + new Vector3(1, -1));
                bool downLeft = vertPositionMap.ContainsKey(position + new Vector3(-1, -1));

                //if the vert is missing any edges it is on the outside
                vertInfo.IsOutsideVert = !up || !right || !down || !left || !upRight || !downright || !upLeft || !downLeft;
                if (vertInfo.IsOutsideVert)
                {
                    edgeVertsSet.Add(position);
                }
                */
            }

            Debug.Log("edge verts : " + edgeVertsSet.Count);

            //get edgeloops
            List<List<Vector3>> OrderedEdgeVerts = new List<List<Vector3>>();
            int iter = 0;
            while (edgeVertsSet.Count > 0)
            {
                Debug.Log("finiding EdgeLoop " + edgeVertsSet.Count);
                OrderedEdgeVerts.Add(ZoneDisplay.FindEdgeLoop(ref edgeVertsSet, ref Edges, ref edgesIntoPoint));
                string str = "i=" + iter + ":";
                foreach (var vert in OrderedEdgeVerts[OrderedEdgeVerts.Count - 1]) { str += vert + " | "; }
                Debug.Log(str);
                iter++;
            }
            
        }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator EditModeTestScriptWithEnumeratorPasses()
        {
            // Use the Assert class to test conditions.
            // Use yield to skip a frame.
            yield return null;
        }
    }
}
