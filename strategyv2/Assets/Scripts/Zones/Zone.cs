using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Zone : IZone
{
    public static int idCnt = 0;
    public int Id { get; set; }
    public List<Rect> BoundingBoxes { get; set; }

    public IZoneBehavior Behavior { get; set; }

    public Zone(List<Rect> boundingBoxes, Color outlineColor)
    {
        BoundingBoxes = boundingBoxes;
        Id = idCnt++;
        Behavior = new EmptyZoneBehavior();
    }

    public Zone(IZone other)
    {
        BoundingBoxes = new List<Rect>(other.BoundingBoxes);
        Id = other.Id;
    }

    public Vector3 GetRandomPoint()
    {
        List<Vector3> randomPoints = new List<Vector3>();
        foreach(var rect in BoundingBoxes)
        {
            float x = Random.Range(rect.xMin, rect.xMax);
            float y = Random.Range(rect.yMin, rect.yMax);
            randomPoints.Add(new Vector3(x, y));
        }

        return randomPoints[Random.Range(0, randomPoints.Count)];
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

    public override bool Equals(object obj)
    {
        var zone = obj as Zone;
        return zone != null && Id == zone.Id;
    }

    public override int GetHashCode()
    {
        var hashCode = 199608042;
        hashCode = hashCode * -1521134295 + Id.GetHashCode();
        hashCode = hashCode * -1521134295 + EqualityComparer<List<Rect>>.Default.GetHashCode(BoundingBoxes);
        return hashCode;
    }
}
