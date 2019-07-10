﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoneDisplay : MonoBehaviour
{
    public Zone DisplayedZone;

    [SerializeField]
    private GameObject _display;

    private void Awake()
    {
        transform.position = Vector3.zero;
    }

    public void Init(Zone zone)
    {
        DisplayedZone = zone;
    }

    public void Change(Vector3 topLeft, Vector3 bottomRight)
    {
        var topLeft_V2 = MapManager.Instance.GetTilePositionFromPosition(topLeft);
        var bottomRight_V2 = MapManager.Instance.GetTilePositionFromPosition(bottomRight);

        Vector2 delta = bottomRight_V2 - topLeft_V2;
        ChangePointSize(new Vector3(topLeft_V2.x, topLeft_V2.y), delta);
    }

    public void ChangePointSize(Vector3 topLeft, Vector3 size)
    {
        
        DisplayedZone.SetRect(new Rect(topLeft, size));
        Rect rect = DisplayedZone.GetRect();

        float xMin = rect.xMin,
            xMax = rect.xMax,
            yMin = rect.yMin,
            yMax = rect.yMax;

        Vector3 topRight = new Vector3(xMax, yMin),
            bottomLeft = new Vector3(xMin, yMax),
            bottomRight = new Vector3(xMax, yMax);

        Vector3 middle = bottomLeft + (topRight - bottomLeft) / 2;
        middle.z = .5f;
        Debug.Log(topLeft);

        _display.transform.position = middle;

        size.z = .1f;
        _display.transform.localScale = size;

    }

    /*
    public void Change(Vector3 topLeft, Vector3 bottomRight)
    {
        var topLeft_V2 = MapManager.Instance.GetTilePositionFromPosition(topLeft);
        var bottomRight_V2 = MapManager.Instance.GetTilePositionFromPosition(bottomRight);

        Vector2 delta = bottomRight_V2 - topLeft_V2;
        Change(topLeft_V2, delta);
    }

    public void Change(Vector2 topLeft, Vector2 size)
    {
        DisplayedZone.SetRect(new Rect(topLeft, size));
        Rect rect = DisplayedZone.GetRect();

        float xMin = rect.xMin, 
            xMax = rect.xMax, 
            yMin = rect.yMin, 
            yMax = rect.yMax;

        Vector3 topRight = new Vector3(xMax, yMin),
            bottomLeft = new Vector3(xMin, yMax), 
            bottomRight = new Vector3(xMax, yMax);

        _lR.SetPosition(0, topLeft);
        _lR.SetPosition(1, topRight);
        _lR.SetPosition(2, bottomRight);
        _lR.SetPosition(3, bottomLeft);
    }
    */
    public void Destroy()
    {

    }
}
