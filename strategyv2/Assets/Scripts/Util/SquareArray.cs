using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SquareArray<T>
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

    public SquareArray(T[] array)
    {
        this.Array = array;
        SideLength = (int)Mathf.Sqrt(Array.Length);
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
}
