using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class AlphaMapChannels
{
    public float[] channels = { 0, 0, 0, 0, 0, 0 };

    const int GrassIndex = 0;
    const int SandIndex = 1;
    const int RockIndex = 2;
    const int RoadIndex = 3;
    const int SnowIndex = 4;
    const int DirtIndex = 5;
    const int MaxIndex = 6;

    public void SetAlphaMapPos(float[,,] alphaData, int x, int y)
    {
        alphaData[y, x, GrassIndex] = channels[GrassIndex];
        alphaData[y, x, SandIndex] = channels[SandIndex];
        alphaData[y, x, RockIndex] = channels[RockIndex];
        alphaData[y, x, RoadIndex] = channels[RoadIndex];
        alphaData[y, x, SnowIndex] = channels[SnowIndex];
        alphaData[y, x, DirtIndex] = channels[DirtIndex];
    }

    public AlphaMapChannels()
    { }

    public AlphaMapChannels(float[] arr)
    {
        for (int i = 0; i < arr.Length; i++)
        {
            channels[i] = arr[i];
        }
    }

    public AlphaMapChannels(Terrain terrain, Improvement improvement, Vector2 gradient)
    {
        Set(terrain, improvement, gradient);
    }

    public void Set(Terrain terrain)
    {
        switch (terrain)
        {
            case Terrain.Water:
                channels[GrassIndex] = 0;
                channels[SandIndex] = 1;
                channels[RockIndex] = 0;
                channels[RoadIndex] = 0;
                channels[SnowIndex] = 0;
                channels[DirtIndex] = 0;
                break;
            case Terrain.Mountain:
                channels[GrassIndex] = 0;
                channels[SandIndex] = 0;
                channels[RockIndex] = 0;
                channels[RoadIndex] = 0;
                channels[SnowIndex] = 1;
                channels[DirtIndex] = 0;
                break;
            case Terrain.Grass:
                channels[GrassIndex] = 0;
                channels[SandIndex] = 0;
                channels[RockIndex] = 0;
                channels[RoadIndex] = 0;
                channels[SnowIndex] = 1;
                channels[DirtIndex] = 0;
                break;
        }
    }
    public void Set(Terrain terrain, Improvement improvement, Vector2 gradient)
    {
        if (channels == null)
        {
            channels = new float[MaxIndex];
        }

        float rockyness = gradient.magnitude * 3;

        switch (terrain)
        {
            case Terrain.Water:
                channels[GrassIndex] = 0;
                channels[SandIndex] = 1;
                channels[RockIndex] = 0;
                channels[RoadIndex] = 0;
                channels[SnowIndex] = 0;
                channels[DirtIndex] = 0;
                break;

            case Terrain.Mountain:
                channels[GrassIndex] = 0;
                channels[SandIndex] = 0;
                channels[RockIndex] = rockyness;
                channels[RoadIndex] = 0;
                channels[SnowIndex] = 1 - rockyness;
                channels[DirtIndex] = 0;
                break;

            case Terrain.Grass:
                rockyness = gradient.magnitude * 10;

                switch (improvement)
                {
                    case Improvement.Desert:
                        channels[GrassIndex] = 0;
                        channels[SandIndex] = 1 - rockyness;
                        channels[RockIndex] = rockyness;
                        channels[RoadIndex] = 0;
                        channels[SnowIndex] = 0;
                        channels[DirtIndex] = 0;
                        break;

                    case Improvement.Road:
                        channels[GrassIndex] = 0;
                        channels[SandIndex] = 0;
                        channels[RockIndex] = 0;
                        channels[RoadIndex] = 1;
                        channels[SnowIndex] = 0;
                        channels[DirtIndex] = 0;
                        break;

                    case Improvement.Town:
                        channels[GrassIndex] = 0;
                        channels[SandIndex] = 0;
                        channels[RockIndex] = 0;
                        channels[RoadIndex] = 0;
                        channels[SnowIndex] = 0;
                        channels[DirtIndex] = 1;
                        break;

                    case Improvement.Farm:
                        channels[GrassIndex] = 0;
                        channels[SandIndex] = 0;
                        channels[RockIndex] = 0;
                        channels[RoadIndex] = 0;
                        channels[SnowIndex] = 0;
                        channels[DirtIndex] = 1;
                        break;

                    default:
                        channels[GrassIndex] = 1 - rockyness;
                        channels[SandIndex] = 0;
                        channels[RockIndex] = rockyness;
                        channels[RoadIndex] = 0;
                        channels[SnowIndex] = 0;
                        break;
                }

                break;
        }
    }

    public void Scale(float weight)
    {
        for (int i = 0; i < channels.Length; i++)
        {
            channels[i] *= weight;
        }
    }

    public void Add(AlphaMapChannels Other)
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