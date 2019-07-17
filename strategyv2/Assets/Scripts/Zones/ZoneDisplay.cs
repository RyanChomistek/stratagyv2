using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

struct VertOutsideEdgeInfo
{
    public Vector3 vert;
    public bool IsOutsideVert;
}

public struct Edge
{
    Vector3 v0, v1;

    public Edge(Vector3 v0, Vector3 v1)
    {
        this.v0 = v0;
        this.v1 = v1;
    }

    public Vector3 GetDisplacement()
    {
        return v1 - v0;
    }

    public override bool Equals(object obj)
    {
        if (!(obj is Edge))
        {
            return false;
        }

        var edge = (Edge)obj;
        return v0.Equals(edge.v0) &&
               v1.Equals(edge.v1);
    }

    public override int GetHashCode()
    {
        var hashCode = -731044709;
        hashCode = hashCode * -1521134295 + EqualityComparer<Vector3>.Default.GetHashCode(v0);
        hashCode = hashCode * -1521134295 + EqualityComparer<Vector3>.Default.GetHashCode(v1);
        return hashCode;
    }
}

public class ZoneDisplay : MonoBehaviour
{
    public Zone DisplayedZone;

    [SerializeField]
    private GameObject _fill;
    [SerializeField]
    private GameObject _outline;
    [SerializeField]
    private float _outlineThickness = .1f;

    [SerializeField]
    private Color _FillColor;
    private string _fillColorShaderProperty = "_Color";
    [SerializeField]
    private Color _OutlineColor;
    private string _outlineColorShaderProperty = "_BaseColor";

    InputController.OnClick OnClickCallback;
    HoverHandler HoverHandler;

    private void Awake()
    {
        transform.position = Vector3.zero;
        _FillColor = new Color(Random.Range(.25f, 1), Random.Range(.25f, 1), Random.Range(.25f, 1), .5f);
        _OutlineColor = InvertColor(_FillColor);
        _OutlineColor.a = .5f;
        
        _fill.GetComponent<Renderer>().material.SetColor(_fillColorShaderProperty, _FillColor);
        _outline.GetComponent<Renderer>().material.SetColor(_outlineColorShaderProperty, _OutlineColor);
    }

    public void MergeZone(ZoneDisplay Other)
    {
        DisplayedZone.BoundingBoxes.AddRange(Other.DisplayedZone.BoundingBoxes);
        ZoneDisplayManager.Instance.DestroyZoneDisplay(Other);
        CreateMeshes();
    }

    #region fill and outline mesh generation
    public void CreateMeshes()
    {
        //make the fill quad
        MeshFilter mf = _fill.GetComponent<MeshFilter>();
        var mesh = new Mesh();
        mf.mesh = mesh;
        List<Vector3> verts = new List<Vector3>();
        List<int> tri = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        HashSet<Edge> Edges = new HashSet<Edge>();
        Dictionary<Vector3, int> vertPositionMap = new Dictionary<Vector3, int>();
        Dictionary<Vector3, List<Edge>> edgesIntoPoint = new Dictionary<Vector3, List<Edge>>();
        List<List<Vector3>> OrderedEdgeVerts = new List<List<Vector3>>();

        CreateMeshesHelper(DisplayedZone.BoundingBoxes, ref verts, ref vertPositionMap, ref Edges, ref edgesIntoPoint, ref OrderedEdgeVerts, ref tri, ref normals, ref uv);

        mesh.vertices = verts.ToArray();
        mesh.triangles = tri.ToArray();
        mesh.normals = normals.ToArray();
        mesh.uv = uv.ToArray();

        //make outline quads
        CreateOutlineMesh(OrderedEdgeVerts);
    }


    public static void CreateMeshesHelper(
        List<Rect> rects,
        ref List<Vector3> verts, 
        ref Dictionary<Vector3, int> vertPositionMap,
        ref HashSet<Edge> Edges, 
        ref Dictionary<Vector3, List<Edge>> edgesIntoPoint,
        ref List<List<Vector3>> OrderedEdgeVerts,
        ref List<int> tri, ref List<Vector3> normals, ref List<Vector2> uv)
    {
        foreach (Rect boundingBox in rects)
        {
            Rect rect = Rect.MinMaxRect(boundingBox.xMin, boundingBox.yMin, boundingBox.xMax, boundingBox.yMax);
            CreateFillQuad(rect, ref vertPositionMap, ref verts, ref Edges, ref edgesIntoPoint, ref tri, ref normals, ref uv);
        }

        //find verts on the outside edge
        List<VertOutsideEdgeInfo> sortedVerts = new List<VertOutsideEdgeInfo>();
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

        //look at every vert, if it is missing any edges, add it to the edge vert set
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
        }

        //get edgeloops
        while (edgeVertsSet.Count > 0)
        {
            OrderedEdgeVerts.Add(FindEdgeLoop(ref edgeVertsSet, ref Edges, ref edgesIntoPoint));
        }
    }

    /// <summary>
    /// sets up finding the edge loop
    /// </summary>
    /// <param name="edgeVertsSet"></param>
    /// <param name="edges"></param>
    /// <param name="edgesIntoPoint"></param>
    /// <returns></returns>
    static public List<Vector3> FindEdgeLoop(ref HashSet<Vector3> edgeVertsSet, ref HashSet<Edge> edges, ref Dictionary<Vector3, List<Edge>> edgesIntoPoint)
    {
        Vector3 currentVert = edgeVertsSet.First();
        var beginning = currentVert;
        List<Vector3> OrderedEdgeVerts = new List<Vector3>();
        OrderedEdgeVerts.AddRange(FindEdgeLoopHelper(currentVert, new HashSet<Vector3>(edgeVertsSet), ref edges, ref edgesIntoPoint));
        foreach(Vector3 vert in OrderedEdgeVerts)
        {
            edgeVertsSet.Remove(vert);
        }

        OrderedEdgeVerts.Add(beginning);

        return OrderedEdgeVerts;
    }
    
    /// <summary>
    /// find the edge loop that goes through every vert in the edge vert set
    /// </summary>
    /// <param name="currentVert"></param>
    /// <param name="edgeVertsSet"></param>
    /// <param name="edges"></param>
    /// <param name="edgesIntoVert"></param>
    /// <returns></returns>
    public static List<Vector3> FindEdgeLoopHelper(Vector3 currentVert, HashSet<Vector3> edgeVertsSet, ref HashSet<Edge> edges, ref Dictionary<Vector3, List<Edge>> edgesIntoVert)
    {
        List<Vector3> path = new List<Vector3>();
        path.Add(currentVert);
        edgeVertsSet.Remove(currentVert);

        Edge edgeUp = new Edge(currentVert, currentVert + Vector3.up),
            edgeRight = new Edge(currentVert, currentVert + Vector3.right),
            edgeDown = new Edge(currentVert, currentVert + Vector3.down),
            edgeLeft = new Edge(currentVert, currentVert + Vector3.left);

        bool up = edgeVertsSet.Contains(currentVert + Vector3.up) && edges.Contains(edgeUp);
        bool right = edgeVertsSet.Contains(currentVert + Vector3.right) && edges.Contains(edgeRight);
        bool down = edgeVertsSet.Contains(currentVert + Vector3.down) && edges.Contains(edgeDown);
        bool left = edgeVertsSet.Contains(currentVert + Vector3.left) && edges.Contains(edgeLeft);
        
        List<Vector3> options = new List<Vector3>();

        //find the possible next moves
        if (up || right || left || down)
        {
            if (up)
            {
                options.Add(currentVert + Vector3.up);
            }

            if (right)
            {
                options.Add(currentVert + Vector3.right);
            }

            if (down)
            {
                options.Add(currentVert + Vector3.down);
            }

            if (left)
            {
                options.Add(currentVert + Vector3.left);
            }
        }

        //if we have no options return back
        if(options.Count == 0)
        {
            return path;
        }

        //find edgeNormal, sum all other incident edges on this vert and normalize
        Vector3 edgeNormal = Vector3.zero;
        List<Edge> incidentEdges = edgesIntoVert[currentVert];

        foreach(var edge in incidentEdges)
        {
            edgeNormal += edge.GetDisplacement();
        }

        edgeNormal = edgeNormal.normalized;

        float smallestAngle = 360;
        Vector3 bestOption = options[0];
        //find the edge that has the smallest angle to edge normal
        //this works in every case except when theres two one by one holes next to each other
        //like this HXH, will result in a like like this H_H not sure how to fix but doesnt seem too bad of a problem
        foreach(var nextVert in options)
        {
            float angle = Vector3.Angle(edgeNormal, nextVert - currentVert);
            if (angle < smallestAngle)
            {
                bestOption = nextVert;
                smallestAngle = angle;
            }
        }
        
        List<Vector3> subpath = FindEdgeLoopHelper(bestOption, edgeVertsSet, ref edges, ref edgesIntoVert);
        path.AddRange(subpath);

        return path;
    }

    private static void AddEdgeToMap(Vector3 vert, Edge edge, ref HashSet<Edge> Edges, ref Dictionary<Vector3, List<Edge>> edgesIntoPoint)
    {
        if(!edgesIntoPoint.ContainsKey(vert))
        {
            edgesIntoPoint[vert] = new List<Edge>();
        }

        edgesIntoPoint[vert].Add(edge);
        Edges.Add(edge);
    }

    /// <summary>
    /// generates an outline of the fill by going through every two points on the edge and drawing a line through them
    /// </summary>
    /// <param name="edgeVerts"></param>
    public void CreateOutlineMesh(List<List<Vector3>> edgeLoops)
    {
        MeshFilter mf = _outline.GetComponent<MeshFilter>();
        var mesh = new Mesh();
        mf.mesh = mesh;
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector3> outlineEdgeVerts = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        
        //make rects
        foreach(var edgeLoop in edgeLoops)
        {
            for (int i = 0; i < edgeLoop.Count - 1; i++)
            {
                int nextVert = i + 1;

                Vector3 v1 = edgeLoop[i], v2 = edgeLoop[nextVert];
                float minX = Mathf.Min(v1.x, v2.x), maxX = Mathf.Max(v1.x, v2.x),
                   minY = Mathf.Min(v1.y, v2.y), maxY = Mathf.Max(v1.y, v2.y);

                var dir = (v2 - v1);
                minY -= _outlineThickness / 2;
                maxY += _outlineThickness / 2;
                minX -= _outlineThickness / 2;
                maxX += _outlineThickness / 2;

                Rect line = Rect.MinMaxRect(minX, minY, maxX, maxY);
                CreateOutlineQuad(line, 0, ref verts, ref tris, ref normals, ref uv);
            }
        }

        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.normals = normals.ToArray();
        mesh.uv = uv.ToArray();
    }

    #region geomatry generation
    /// <summary>
    /// creates a quad with the given rarams
    /// </summary>
    /// <param name="rect"></param>
    /// <param name="vertices"></param>
    /// <param name="edgeVertices"> returns all of the verticies on the ouside edge of the quad in counterclockwise order</param>
    /// <param name="tri"></param>
    /// <param name="normals"></param>
    /// <param name="uv"></param>
    public static void CreateFillQuad(Rect rect,
        ref Dictionary<Vector3, int> vertPositionMap,
        ref List<Vector3> vertices,
        ref HashSet<Edge> Edges,
        ref Dictionary<Vector3, List<Edge>> edgesIntoPoint,
        ref List<int> tri,
        ref List<Vector3> normals,
        ref List<Vector2> uv)
    {
        int width = Mathf.RoundToInt(rect.width);
        int height = Mathf.RoundToInt(rect.height);
        Vector3 bottomLeft = rect.min;
        int startVert = vertices.Count;

        //generate verts
        for (int x = 0; x <= width; x++)
        {
            for (int y = 0; y <= height; y++)
            {
                var position = new Vector3(bottomLeft.x + x, bottomLeft.y + y, 0);
                if (!vertPositionMap.ContainsKey(position))
                {
                    vertices.Add(position);
                    uv.Add(new Vector2(x, y));
                    vertPositionMap.Add(position, vertices.Count - 1);
                }
            }
        }

        //make tris
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 bottomLeftPos = new Vector3(bottomLeft.x + x, bottomLeft.y + y, 0);
                Vector3 bottomRightPos = bottomLeftPos + new Vector3(1, 0);
                Vector3 topRightPos = bottomLeftPos + new Vector3(1, 1);
                Vector3 topLeftPos = bottomLeftPos + new Vector3(0, 1);

                int bottomLeftIndex = vertPositionMap[bottomLeftPos];
                int bottomRightIndex = vertPositionMap[bottomRightPos];
                int topRightIndex = vertPositionMap[topRightPos];
                int topLeftIndex = vertPositionMap[topLeftPos];

                tri.Add(bottomLeftIndex);
                tri.Add(topLeftIndex);
                tri.Add(bottomRightIndex);

                tri.Add(topLeftIndex);
                tri.Add(topRightIndex);
                tri.Add(bottomRightIndex);

                Vector3[] vertsInQuad = { bottomLeftPos, bottomRightPos, topRightPos, topLeftPos };

                //add every possible edge, and setup vert to input edge map
                foreach (var start in vertsInQuad)
                {
                    foreach (var end in vertsInQuad)
                    {
                        if (start != end)
                        {
                            Edge edge = new Edge(start, end);
                            AddEdgeToMap(end, edge, ref Edges, ref edgesIntoPoint);
                        }
                    }
                }
            }
        }

        normals.Clear();
        foreach (var vert in vertices)
        {
            normals.Add(-Vector3.forward);
        }
    }

    public void CreateOutlineQuad(Rect rect, float padding, ref List<Vector3> vertices, ref List<int> tri, ref List<Vector3> normals, ref List<Vector2> uv)
    {
        int startVert = vertices.Count;

        vertices.Add(new Vector3(rect.xMin - padding, rect.yMin - padding, 0));
        vertices.Add(new Vector3(rect.xMax + padding, rect.yMin - padding, 0));
        vertices.Add(new Vector3(rect.xMin - padding, rect.yMax + padding, 0));
        vertices.Add(new Vector3(rect.xMax + padding, rect.yMax + padding, 0));
        
        tri.Add(startVert);
        tri.Add(startVert + 2);
        tri.Add(startVert + 1);

        tri.Add(startVert + 2);
        tri.Add(startVert + 3);
        tri.Add(startVert + 1);

        normals.Add(-Vector3.forward);
        normals.Add(-Vector3.forward);
        normals.Add(-Vector3.forward);
        normals.Add(-Vector3.forward);

        uv.Add(new Vector2(0, 0));
        uv.Add(new Vector2(1, 0));
        uv.Add(new Vector2(0, 1));
        uv.Add(new Vector2(1, 1));
    }
    #endregion

    #endregion

    public Color InvertColor(Color color)
    {
        int r = (int) (color.r * 255), g = (int)(color.g * 255), b = (int)(color.b * 255);
        return new Color((0xff ^ r) / 255f, (0xff ^ g) / 255f, (0xff ^ b) / 255f);
    }

    public void Init(Zone zone)
    {
        DisplayedZone = zone;
        OnClickCallback = mousePosition =>
        {
            var tileCoordinate = MapManager.Instance.GetTilePositionFromPosition(mousePosition);
            if (DisplayedZone.Contains(tileCoordinate))
            {
                ZoneDisplayManager.Instance.OnZoneSelected(this);
            }
        };

        InputController.Instance.RegisterOnClickCallBack(OnClickCallback);

        HoverHandler = new ConditionalHoverHandler(
            //warmups
            (x, y) => {  }, (x, y) => {},
            //start
            (x, y) => {
                //reverse the fill and outline colors
                _fill.GetComponent<Renderer>().material.SetColor(_fillColorShaderProperty, _OutlineColor);
                _outline.GetComponent<Renderer>().material.SetColor(_outlineColorShaderProperty, _FillColor);
            }, 
            (x, y) => {}, 
            //end
            (x, y) => {
                _fill.GetComponent<Renderer>().material.SetColor(_fillColorShaderProperty, _FillColor);
                _outline.GetComponent<Renderer>().material.SetColor(_outlineColorShaderProperty, _OutlineColor);
            },
            //condition for when hovering should trigger
            (x) => {
                Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                var tileCoordinate = MapManager.Instance.GetTilePositionFromPosition(mousePosition);
                return DisplayedZone.Contains(tileCoordinate);
            },
            .1f);

        InputController.Instance.RegisterHoverHandler(HoverHandler);
    }

    public void Change(Vector3 topLeft, Vector3 bottomRight, int rectIndex)
    {
        var topLeft_V2 = MapManager.Instance.GetTilePositionFromPosition(topLeft);
        var bottomRight_V2 = MapManager.Instance.GetTilePositionFromPosition(bottomRight);

        Vector2 delta = bottomRight_V2 - topLeft_V2;
        ChangePointSize(new Vector3(topLeft_V2.x, topLeft_V2.y), delta, rectIndex);
    }

    public void ChangePointSize(Vector3 topLeft, Vector3 size, int rectIndex)
    {
        Rect rect = new Rect(topLeft, size);
        
        float xMin = rect.xMin,
            xMax = rect.xMax,
            yMin = rect.yMin,
            yMax = rect.yMax;
        
        float worldxMin = Mathf.Min(xMin, xMax), worldxMax = Mathf.Max(xMin, xMax), 
            worldyMin = Mathf.Min(yMin, yMax), worldyMax = Mathf.Max(yMin, yMax);
        
        topLeft = new Vector3(worldxMin, worldyMax);

        Vector3 topRight = new Vector3(worldxMax, worldyMax),
            bottomLeft = new Vector3(worldxMin, worldyMin),
            bottomRight = new Vector3(worldxMax, worldyMin);
        
        rect.position = bottomLeft;
        rect.position += new Vector2(-.5f, -.5f);

        //need to add 1 to x to fix obo error in the rect contains function
        rect.size = topRight - bottomLeft + new Vector3(1,1,0);
        DisplayedZone.BoundingBoxes[rectIndex] = rect;
        CreateMeshes();
    }
    
    public void OnDestroy()
    {
        //free input handelers
        InputController.Instance.UnRegisterOnClickCallBack(OnClickCallback);
        InputController.Instance.UnRegisterHoverHandler(HoverHandler);
    }
}
