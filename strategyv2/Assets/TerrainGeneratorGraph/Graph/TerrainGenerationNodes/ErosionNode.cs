using HeightMapGeneration;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class ErosionNode : TerrainNode
{
    [Input] public ComputeShader erosion;
    [Input] public int MaxNumThreads = 65535;
    [Input] public int numDropletsPerCell = 8;
    [Input] public int maxLifetime = 30;
    [Input] public float sedimentCapacityFactor = 3;
    [Input] public float minSedimentCapacity = .01f;
    [Input] public float depositSpeed = 0.3f;
    [Input] public float erodeSpeed = 0.3f;
    [Input] public float evaporateSpeed = .01f;
    [Input] public float gravity = 4;
    [Input] public float startSpeed = 1;
    [Input] public float startWater = 1;
    [Input] public float inertia = 0.3f;

    [Input] public float[] InputHeightMap;
    [Input] public float[] InputWaterMap;

    [Output] public float[] HeightMap;
    [Output] public float[] WaterMap;
    public int HeightMapSize = 128 * 128;

    public override object GetValue(XNode.NodePort port)
    {
        if(port.fieldName == "HeightMap")
            return HeightMap;

        if (port.fieldName == "WaterMap")
            return WaterMap;

        return null;
    }

    public override void Recalculate()
    {
        ComputeShader erosion = GetInputValue("erosion", this.erosion);
        int MaxNumThreads = GetInputValue("MaxNumThreads", this.MaxNumThreads);
        int numDropletsPerCell = GetInputValue("numDropletsPerCell", this.numDropletsPerCell);
        int maxLifetime = GetInputValue("maxLifetime", this.maxLifetime);
        float sedimentCapacityFactor = GetInputValue("sedimentCapacityFactor", this.sedimentCapacityFactor);
        float minSedimentCapacity = GetInputValue("minSedimentCapacity", this.minSedimentCapacity);
        float depositSpeed = GetInputValue("depositSpeed", this.depositSpeed);
        float erodeSpeed = GetInputValue("erodeSpeed", this.erodeSpeed);
        float evaporateSpeed = GetInputValue("evaporateSpeed", this.evaporateSpeed);
        float gravity = GetInputValue("gravity", this.gravity);
        float startSpeed = GetInputValue("startSpeed", this.startSpeed);
        float startWater = GetInputValue("startWater", this.startWater);
        float inertia = GetInputValue("inertia", this.inertia);

        float[] InputHeightMap = GetInputValue("InputHeightMap", this.InputHeightMap);
        float[] InputWaterMap = GetInputValue("InputWaterMap", this.InputWaterMap);

        if (InputHeightMap != null && InputWaterMap != null && InputHeightMap.Length > 0 && InputWaterMap.Length > 0)
        {
            ErosionSettings ES = new ErosionSettings()
            {
                erosion = erosion,
                MaxNumThreads = MaxNumThreads,
                numDropletsPerCell = numDropletsPerCell,
                maxLifetime = maxLifetime,
                sedimentCapacityFactor = sedimentCapacityFactor,
                minSedimentCapacity = minSedimentCapacity,
                depositSpeed = depositSpeed,
                erodeSpeed = erodeSpeed,
                evaporateSpeed = evaporateSpeed,
                gravity = gravity,
                startSpeed = startSpeed,
                startWater = startWater,
                inertia = inertia,
            };

            ErosionOutput EO = ErosionHeightMapLayer.Run(ES, InputHeightMap, InputWaterMap);

            HeightMap = EO.HeightMap;
            WaterMap = EO.WaterMap;

            HeightMapSize = InputHeightMap.Length;

            base.Recalculate();
        }
    }
}