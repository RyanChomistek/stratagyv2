using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoneDisplay : MonoBehaviour
{
    public Zone DisplayedZone;

    [SerializeField]
    private GameObject _display;
    [SerializeField]
    private Color _FillColor;
    [SerializeField]
    private Color _OutlineColor;
    private void Awake()
    {
        transform.position = Vector3.zero;
        _FillColor = new Color(Random.Range(.25f, 1), Random.Range(.25f, 1), Random.Range(.25f, 1), .5f);
        
        _OutlineColor = InvertColor(_FillColor);
        _OutlineColor.a = .5f;
        _display.GetComponent<Renderer>().material.SetColor("_OutlineColor", _OutlineColor);
        _display.GetComponent<Renderer>().material.SetColor("_FillColor", _FillColor);
        _display.GetComponent<Renderer>().material.SetVector("_ObjectScale", _display.transform.localScale);
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
                _display.GetComponent<Renderer>().material.SetColor("_OutlineColor", _FillColor);
                _display.GetComponent<Renderer>().material.SetColor("_FillColor", _OutlineColor);
            }, 
            (x, y) => {}, 
            //end
            (x, y) => {
                _display.GetComponent<Renderer>().material.SetColor("_OutlineColor", _OutlineColor);
                _display.GetComponent<Renderer>().material.SetColor("_FillColor", _FillColor);
            },
            //condition for when hovering should trigger
            (x) => {
                Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                var tileCoordinate = MapManager.Instance.GetTilePositionFromPosition(mousePosition);
                return DisplayedZone.Contains(tileCoordinate);
            },
            .5f);

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
        
        Vector3 middle = bottomLeft + (topRight - bottomLeft) / 2;
        middle.z = .5f;

        _display.transform.position = middle;

        size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), 1f);
        //add a one vector to account for the display starting in the middle of a tile
        _display.transform.localScale = size + new Vector3(1,1,0);
        _display.GetComponent<Renderer>().material.SetVector("_ObjectScale", _display.transform.localScale);
    }
    
    public void Destroy()
    {

    }
}
