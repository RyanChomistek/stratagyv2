using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterNode : TerrainNode
{
    [Input] public ComputeShader Droplets;
    [Input] public int MaxNumThreads = 65535;
    [Input] public int maxLifetime = 30;
    [Input] public float evaporateSpeed = .01f;
    [Input] public float gravity = 4;
    [Input] public float startSpeed = 1;
    [Input] public float startWater = 1;
    [Input] public float inertia = 0.3f;

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
        float evaporateSpeed = GetInputValue("evaporateSpeed", this.evaporateSpeed);
        float gravity = GetInputValue("gravity", this.gravity);
        float startSpeed = GetInputValue("startSpeed", this.startSpeed);
        float startWater = GetInputValue("startWater", this.startWater);
        float inertia = GetInputValue("inertia", this.inertia);

        float[] InputHeightMap = GetInputValue("InputHeightMap", this.InputHeightMap);
        float[] InputWaterMap = GetInputValue("InputWaterMap", this.InputWaterMap);

        if(IsInputArrayValid(InputHeightMap) && IsInputArrayValid(InputWaterMap))
        {
            ComputeBuffer mapBuffer = new ComputeBuffer(InputHeightMap.Length, sizeof(float));
            mapBuffer.SetData(InputHeightMap);
            Droplets.SetBuffer(0, "heightMap", mapBuffer);

            // WaterMap buffer
            ComputeBuffer waterMapBuffer = new ComputeBuffer(InputWaterMap.Length, sizeof(float));
            waterMapBuffer.SetData(InputWaterMap);
            Droplets.SetBuffer(0, "waterMap", waterMapBuffer);

            int numThreads = System.Math.Min(InputHeightMap.Length, MaxNumThreads);
            int numElementsToProcess = Mathf.CeilToInt(InputHeightMap.Length / (float)numThreads);

            Droplets.SetInt("mapSize", new SquareArray<float>(InputWaterMap).SideLength);
            Droplets.SetInt("numThreads", numThreads);
            Droplets.SetInt("numElementsToProcess", numElementsToProcess);
            Droplets.SetInt("maxLifetime", maxLifetime);
            Droplets.SetFloat("inertia", inertia);
            Droplets.SetFloat("evaporateSpeed", evaporateSpeed);
            Droplets.SetFloat("gravity", gravity);
            Droplets.SetFloat("startSpeed", startSpeed);
            Droplets.SetFloat("startWater", startWater);

            Droplets.Dispatch(0, numThreads, 1, 1);

            WaterMapOutput = (float[]) InputWaterMap.Clone();
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
