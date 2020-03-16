using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SquareArray<T> : System.ICloneable
{
    public T[] Array { private set; get; }

    public int SideLength = -1;
    public int Length { get { return Array.Length; } }

    public T this[int x, int y]
    {
        get { return Array[(y * SideLength) + x]; }
        set { Array[(y * SideLength) + x] = (T) value; }
    }

    public T this[int i]
    {
        get { return Array[i]; }
        set { Array[i] = (T) value; }
    }    
    
    public T this[Vector2Int vec2]
    {
        get { return this[vec2.x, vec2.y]; }
        set { this[vec2.x, vec2.y] = (T) value; }
    }

    public SquareArray(T[] array)
    {
        this.Array = array;
        if(array != null)
            SideLength = (int)Mathf.Sqrt(Array.Length);
    }

    public SquareArray(int sideLength)
    {
        this.Array = new T[sideLength * sideLength];
        SideLength = sideLength;
    }

    public SquareArray(int sideLength, T defaultValue)
    {
        this.Array = new T[sideLength * sideLength];
        SideLength = sideLength;

        for (int i = 0; i < Array.Length; i++)
        {
            Array[i] = defaultValue;
        }
    }

    public int Convert2DCoordinateTo1D(Vector2Int loc)
    {
        return (loc.y * SideLength) + loc.x;
    }

    public int Convert2DCoordinateTo1D(int x, int y)
    {
        return (y * SideLength) + x;
    }

    public bool InBounds(int x, int y)
    {
        if (x < SideLength &&
            y < SideLength &&
            x >= 0 &&
            y >= 0)
        {
            return true;
        }

        return false;
    }

    public bool InBounds(Vector2Int position)
    {
        //Debug.Log($"{position}{map.GetUpperBound(0)} {position.x <= map.GetUpperBound(0)} { position.y <= map.GetUpperBound(1)} { position.x > 0} { position.y > 0} ");
        if (position.x < SideLength &&
            position.y < SideLength &&
            position.x >= 0 &&
            position.y >= 0)
        {
            return true;
        }

        return false;
    }

    public T[,] To2D()
    {
        T[,] twoDMap = new T[SideLength, SideLength];

        //for (int i = 0; i < Array.Length; i++)
        //{
        //    int x = i % SideLength;
        //    int y = i / SideLength;
        //    twoDMap[x, y] = Array[i];
        //}
        for (int x = 0; x < SideLength; x++)
        {
            for (int y = 0; y < SideLength; y++)
            {
                twoDMap[x, y] = this[x, y];
            }
        }


        return twoDMap;
    }

    public object Clone()
    {
        return new SquareArray<T>((T[]) Array.Clone());
    }
}
