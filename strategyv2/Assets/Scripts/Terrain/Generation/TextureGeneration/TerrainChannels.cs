using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainChannels
{
    public float[] channels = new float[(int) Terrain.Max];

    public TerrainChannels()
    { }

    public void Set(Terrain terrain)
    {
        channels[(int)terrain] = 1;
    }

    public void Scale(float weight)
    {
        for (int i = 0; i < channels.Length; i++)
        {
            channels[i] *= weight;
        }
    }

    public void Add(TerrainChannels Other)
    {
        for (int i = 0; i < channels.Length; i++)
        {
            channels[i] += Other.channels[i];
        }
    }

    public void Normalize()
    {
        float totalLen = 0;
        for (int i = 0; i < channels.Length; i++)
        {
            totalLen += channels[i] * channels[i];
        }

        totalLen = Mathf.Sqrt(totalLen);

        for (int i = 0; i < channels.Length; i++)
        {
            channels[i] /= totalLen;
        }
    }
}
