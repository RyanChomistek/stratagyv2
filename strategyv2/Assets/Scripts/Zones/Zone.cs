using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Zone
{
    public List<Rect> BoundingBoxes { get; set; }
    Color OutlineColor;

    public Zone(List<Rect> boundingBoxes, Color outlineColor)
    {
        BoundingBoxes = boundingBoxes;
        OutlineColor = outlineColor;
    }

    public bool Contains(Vector3 point)
    {
        return Contains(new Vector2(point.x, point.y));
    }

    public bool Contains(Vector2 point)
    {
        foreach(Rect rect in BoundingBoxes)
        {
            if (rect.Contains(point))
            {
                return true;
            }
        }

        return false;
    }

    public bool Overlaps(Zone other)
    {
        foreach(Rect r1 in BoundingBoxes)
        {
            foreach (Rect r2 in other.BoundingBoxes)
            {
                if(r1.Overlaps(r2))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
