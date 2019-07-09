using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Zone : IZone
{
    Rect BoundingBox;
    Color OutlineColor;


    public Zone(Rect boundingBox, Color outlineColor)
    {
        BoundingBox = boundingBox;
        OutlineColor = outlineColor;
    }

    public Rect GetRect()
    {
        return BoundingBox;
    }

    public void SetRect(Rect boundingBox)
    {
        BoundingBox = boundingBox;
    }
}
