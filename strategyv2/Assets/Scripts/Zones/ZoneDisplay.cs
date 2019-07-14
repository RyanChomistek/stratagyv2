using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    private void Awake()
    {
        transform.position = Vector3.zero;
        _FillColor = new Color(Random.Range(.25f, 1), Random.Range(.25f, 1), Random.Range(.25f, 1), .5f);
        _OutlineColor = InvertColor(_FillColor);
        _OutlineColor.a = .5f;



        _fill.GetComponent<Renderer>().material.SetColor(_fillColorShaderProperty, _FillColor);
        _outline.GetComponent<Renderer>().material.SetColor(_outlineColorShaderProperty, _OutlineColor);
    }

    public void CreateMeshes()
    {
        //make the fill quad
        MeshFilter mf = _fill.GetComponent<MeshFilter>();
        var mesh = new Mesh();
        mf.mesh = mesh;
        List<Vector3> verts = new List<Vector3>();
        List<Vector3> edgeVerts = new List<Vector3>();
        List<int> tri = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();

        Rect rect = DisplayedZone.BoundingBoxes[0];
        rect = Rect.MinMaxRect(rect.xMin, rect.yMin, rect.xMax-1, rect.yMax); 

        CreateQuad(rect, -_outlineThickness + .5f, ref verts, ref edgeVerts, ref tri, ref normals, ref uv);
        mesh.vertices = verts.ToArray();
        mesh.triangles = tri.ToArray();
        mesh.normals = normals.ToArray();
        mesh.uv = uv.ToArray();

        //make outline quads
        CreateOutlineMesh(edgeVerts);
    }

    /// <summary>
    /// generates an outline of the fill by going through every two points on the edge and drawing a line through them
    /// </summary>
    /// <param name="edgeVerts"></param>
    public void CreateOutlineMesh(List<Vector3> edgeVerts)
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
        for (int i = 0; i < edgeVerts.Count; i++)
        {
            int nextVert = i + 1; 
            if(i == edgeVerts.Count - 1)
            {
                nextVert = 0;
            }

            Vector3 v1 = edgeVerts[i], v2 = edgeVerts[nextVert];
            float minX = Mathf.Min(v1.x, v2.x), maxX = Mathf.Max(v1.x, v2.x),
               minY = Mathf.Min(v1.y, v2.y), maxY = Mathf.Max(v1.y, v2.y);

            var dir = (v2 - v1);
            if(Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            {
                //horizontal line
                minY -= _outlineThickness/2;
                maxY += _outlineThickness/2;
                minX -= _outlineThickness / 2;
                maxX += _outlineThickness / 2;

            }
            else
            {
                //vertical line
                minX -= _outlineThickness/2;
                maxX += _outlineThickness/2;
                minY -= _outlineThickness / 2;
                maxY += _outlineThickness / 2;
            }

            Rect line = Rect.MinMaxRect(minX, minY, maxX, maxY);
            CreateQuad(line, 0, ref verts, ref outlineEdgeVerts, ref tris, ref normals, ref uv);
        }

        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.normals = normals.ToArray();
        mesh.uv = uv.ToArray();
    }

    /// <summary>
    /// creates a quad with the given rarams
    /// </summary>
    /// <param name="rect"></param>
    /// <param name="vertices"></param>
    /// <param name="edgeVertices"> returns all of the verticies on the ouside edge of the quad in counterclockwise order</param>
    /// <param name="tri"></param>
    /// <param name="normals"></param>
    /// <param name="uv"></param>
    public void CreateQuad(Rect rect, float padding, ref List<Vector3> vertices, ref List<Vector3> edgeVertices, ref List<int> tri, ref List<Vector3> normals, ref List<Vector2> uv)
    {
        int startVert = vertices.Count;

        vertices.Add(new Vector3(rect.xMin - padding, rect.yMin - padding, 0));
        vertices.Add(new Vector3(rect.xMax + padding, rect.yMin -padding, 0));
        vertices.Add(new Vector3(rect.xMin - padding, rect.yMax + padding, 0));
        vertices.Add(new Vector3(rect.xMax + padding, rect.yMax + padding, 0));

        edgeVertices.Add(new Vector3(rect.xMin - padding, rect.yMin - padding, 0));
        edgeVertices.Add(new Vector3(rect.xMax + padding, rect.yMin - padding, 0));
        edgeVertices.Add(new Vector3(rect.xMax + padding, rect.yMax + padding, 0));
        edgeVertices.Add(new Vector3(rect.xMin - padding, rect.yMax + padding, 0));

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
        
        uv.Add(new Vector2(0,0));
        uv.Add(new Vector2(1,0));
        uv.Add(new Vector2(0,1));
        uv.Add(new Vector2(1,1));
    }

    public Color InvertColor(Color color)
    {
        int r = (int) (color.r * 255), g = (int)(color.g * 255), b = (int)(color.b * 255);
        Debug.Log($"{r} {0xff ^ r} | {g} {0xff ^ g} | {b} {0xff ^ b}");
        return new Color((0xff ^ r) / 255f, (0xff ^ g) / 255f, (0xff ^ b) / 255f);
    }

    public void Init(Zone zone)
    {
        DisplayedZone = zone;
        
        InputController.Instance.RegisterOnClickCallBack(mousePosition =>
        {
            var tileCoordinate = MapManager.Instance.GetTilePositionFromPosition(mousePosition);
            if (DisplayedZone.Contains(tileCoordinate))
            {
                ZoneDisplayManager.Instance.OnZoneSelected(this);
            }
        });

        HoverHandler handler = new ConditionalHoverHandler(
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

        InputController.Instance.RegisterHoverHandler(handler);
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
        //need to add 1 to x to fix obo error in the rect contains function
        rect.size = topRight - bottomLeft + new Vector3(1,0,0);
        DisplayedZone.BoundingBoxes[rectIndex] = rect;
        CreateMeshes();
    }
    
    public void Destroy()
    {

    }
}
