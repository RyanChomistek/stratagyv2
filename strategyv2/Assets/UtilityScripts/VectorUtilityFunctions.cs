using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VectorUtilityFunctions
{
    static public Vector2 Vec2IntToVec2(Vector2Int v2i)
    {
        return new Vector2(v2i.x, v2i.y)
;    }

    public static Vector2Int CeilVector(Vector2 vec)
    {
        return new Vector2Int(Mathf.CeilToInt(vec.x), Mathf.CeilToInt(vec.y));
    }

    public static Vector2Int FloorVector(Vector2 vec)
    {
        return new Vector2Int(Mathf.FloorToInt(vec.x), Mathf.FloorToInt(vec.y));
    }

    public static Vector2Int RoundVector(Vector2 vec)
    {
        return new Vector2Int(Mathf.RoundToInt(vec.x), Mathf.RoundToInt(vec.y));
    }

    public static Vector2 DivScaler(Vector2 vec, float val)
    {
        return new Vector2(vec.x / val, vec.y / val);
    }

    public static Vector3 DivScaler(Vector3 vec, float val)
    {
        return new Vector3(vec.x / val, vec.y / val, vec.z / val);
    }
}
