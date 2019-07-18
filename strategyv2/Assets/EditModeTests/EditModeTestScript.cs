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
        public void TestFindEdgeLoop()
        {
            List<Vector3> verts = new List<Vector3>();
            List<int> tri = new List<int>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            HashSet<Edge> Edges = new HashSet<Edge>();
            Dictionary<Vector3, int> vertPositionMap = new Dictionary<Vector3, int>();
            Dictionary<Vector3, HashSet<Edge>> edgesIntoPoint = new Dictionary<Vector3, HashSet<Edge>>();
            List<List<Vector3>> OrderedEdgeVerts = new List<List<Vector3>>();

            List<Rect> rects = new List<Rect>();

            rects.Add(new Rect(new Vector2(61.5f, 29.5f), new Vector2(6, 1)));
            rects.Add(new Rect(new Vector2(58.5f, 27.5f), new Vector2(5, 5)));
            rects.Add(new Rect(new Vector2(64.5f, 27.5f), new Vector2(1, 5)));

            ZoneDisplay.CreateMeshesHelper(rects, ref verts, ref vertPositionMap, ref Edges, ref edgesIntoPoint, ref OrderedEdgeVerts, ref tri, ref normals, ref uv);

        }

        [Test]
        public void TestFindEdgeLoop2()
        {
            List<Vector3> verts = new List<Vector3>();
            List<int> tri = new List<int>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            HashSet<Edge> Edges = new HashSet<Edge>();
            Dictionary<Vector3, int> vertPositionMap = new Dictionary<Vector3, int>();
            Dictionary<Vector3, HashSet<Edge>> edgesIntoPoint = new Dictionary<Vector3, HashSet<Edge>>();
            List<List<Vector3>> OrderedEdgeVerts = new List<List<Vector3>>();

            List<Rect> rects = new List<Rect>();

            rects.Add(new Rect(new Vector2(62.5f, 27.5f), new Vector2(1, 3)));
            rects.Add(new Rect(new Vector2(66.5f, 27.5f), new Vector2(1, 3)));
            rects.Add(new Rect(new Vector2(62.5f, 27.5f), new Vector2(5, 1)));
            rects.Add(new Rect(new Vector2(64.5f, 27.5f), new Vector2(1, 3)));
            rects.Add(new Rect(new Vector2(62.5f, 29.5f), new Vector2(5, 1)));

            ZoneDisplay.CreateMeshesHelper(rects, ref verts, ref vertPositionMap, ref Edges, ref edgesIntoPoint, ref OrderedEdgeVerts, ref tri, ref normals, ref uv);

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
