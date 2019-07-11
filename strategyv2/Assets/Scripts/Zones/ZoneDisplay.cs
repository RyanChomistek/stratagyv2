using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoneDisplay : MonoBehaviour
{
    public Zone DisplayedZone;

    [SerializeField]
    private GameObject _display;
    [SerializeField]
    private Color _color;
    private void Awake()
    {
        transform.position = Vector3.zero;
        _color = new Color(Random.Range(.5f, 1), Random.Range(.5f, 1), Random.Range(.5f, 1), 1);
        _display.GetComponent<Renderer>().material.SetColor("Color_890189E8", _color);
    }

    public void Init(Zone zone)
    {
        DisplayedZone = zone;
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
        
        Vector3 middle = bottomLeft + (topRight - bottomLeft) / 2;
        middle.z = .5f;

        _display.transform.position = middle;

        size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), .1f);
        //add a one vector to account for the display starting in the middle of a tile
        _display.transform.localScale = size + new Vector3(1,1);

    }
    
    public void Destroy()
    {

    }
}
