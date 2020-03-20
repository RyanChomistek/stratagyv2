using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateNodeMenu("TileNodes/Lakes")]
public class WaterNode : TerrainNode
{
    [Input] public ComputeShader Droplets;
    [Input] public int MaxNumThreads = 65535;
    [Input] public int maxLifetime = 30;
    [Input] public int numDropletsPerTile = 1;
    [Input] public float evaporateSpeed = .01f;
    [Input] public float gravity = 4;
    [Input] public float startSpeed = 1;
    [Input] public float startWater = 1;
    [Input] public float inertia = 0.3f;
    [Input] public float RunWeight = 0.5f;
    [Input] public float StopWeight = 0.5f;

    [Input] public float[] InputHeightMap = null;
    [Input] public float[] InputWaterMap = null;

    [Output] public float[] WaterMapOutput = null;

    public override object GetValue(XNode.NodePort port)
    {
        if (port.fieldName == "WaterMapOutput")
            return WaterMapOutput;

        return null;
    }

    public override void Recalculate()
    {
        ComputeShader Droplets = GetInputValue("Droplets", this.Droplets);
        int MaxNumThreads = GetInputValue("MaxNumThreads", this.MaxNumThreads);
        int maxLifetime = GetInputValue("maxLifetime", this.maxLifetime);
        int numDropletsPerTile = GetInputValue("numDropletsPerTile", this.numDropletsPerTile);
        float evaporateSpeed = GetInputValue("evaporateSpeed", this.evaporateSpeed);
        float gravity = GetInputValue("gravity", this.gravity);
        float startSpeed = GetInputValue("startSpeed", this.startSpeed);
        float startWater = GetInputValue("startWater", this.startWater);
        float inertia = GetInputValue("inertia", this.inertia);
        float RunWeight = GetInputValue("RunWeight", this.RunWeight);
        float StopWeight = GetInputValue("StopWeight", this.StopWeight);

        float[] InputHeightMap = GetInputValue("InputHeightMap", this.InputHeightMap);
        float[] InputWaterMap = GetInputValue("InputWaterMap", this.InputWaterMap);

        if(IsInputArrayValid(InputHeightMap) && IsInputArrayValid(InputWaterMap))
        {
            ComputeBuffer mapBuffer = new ComputeBuffer(InputHeightMap.Length, sizeof(float));
            mapBuffer.SetData(InputHeightMap);
            Droplets.SetBuffer(0, "heightMap", mapBuffer);

            // WaterMap buffer
            float[] temp = (float[])InputWaterMap.Clone();
            ComputeBuffer waterMapBuffer = new ComputeBuffer(temp.Length, sizeof(float));
            waterMapBuffer.SetData(temp);
            Droplets.SetBuffer(0, "waterMap", waterMapBuffer);

            // Random Seeds
            int[] seeds = new int[]
            {
                Graph.Rand.Next(0, 42949672),
                Graph.Rand.Next(0, 4008679),
                Graph.Rand.Next(0, 64035029),
                Graph.Rand.Next(0, 24038167)
            };

            int numDroplets = numDropletsPerTile * temp.Length;
            int numThreads = System.Math.Min(numDroplets, MaxNumThreads);
            int numDropletsPerThread = Mathf.CeilToInt(numDroplets / (float)numThreads);

            Droplets.SetInt("mapSize", new SquareArray<float>(InputWaterMap).SideLength);
            Droplets.SetInt("numThreads", numThreads);
            Droplets.SetInt("numDropletsPerThread", numDropletsPerThread);
            Droplets.SetInt("maxLifetime", maxLifetime);
            Droplets.SetFloat("inertia", inertia);
            Droplets.SetFloat("evaporateSpeed", evaporateSpeed);
            Droplets.SetFloat("gravity", gravity);
            Droplets.SetFloat("startSpeed", startSpeed);
            Droplets.SetFloat("startWater", startWater);
            Droplets.SetFloat("RunWeight", RunWeight);
            Droplets.SetFloat("StopWeight", StopWeight);
            Droplets.SetInts("seed", seeds);

            Droplets.Dispatch(0, numThreads, 1, 1);

            WaterMapOutput = new float[InputWaterMap.Length];
            waterMapBuffer.GetData(WaterMapOutput);

            mapBuffer.Release();
            waterMapBuffer.Release();
        }
    }

    public override void Flush()
    {
        InputHeightMap = null;
        InputWaterMap = null;
        WaterMapOutput = null;
    }
}
