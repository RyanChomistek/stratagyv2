using HeightMapGeneration;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[System.Serializable]
public class TerrainNode : XNode.Node
{
    bool dirty;

    public bool GetInputAndCheckForDirty<T>(string name, T initial, out T output)
    {
        T temp = GetInputValue<T>(name, initial);
        bool dirty = !initial.Equals(temp);
        output = temp;
        return dirty;
    }

    public virtual void Recalculate()
    {
        // loop through all output ports and propogate recalc
        foreach(var port in this.Ports)
        {
            if(port.IsOutput)
            {
                List<NodePort> connections = port.GetConnections();
                foreach (var connection in connections)
                {
                    TerrainNode TNode;
                    if (TNode = connection.node as TerrainNode)
                    {
                        TNode.Recalculate();
                    }
                }
            }
        }
    }

    public override void OnCreateConnection(NodePort from, NodePort to)
    {
        Recalculate();
    }

    public override void OnRemoveConnection(NodePort port)
    {
        Recalculate();
    }
}

public class IntNode: TerrainNode
{
    [Input] public int i;
    [Output] public int o;

    public override object GetValue(XNode.NodePort port)
    {
        return i;
    }
}

[System.Serializable]
public class ArrayNode : TerrainNode
{
    [Input] public int Size;
    public int SizeSquared;
    [Output] public float[] Array;
    public override object GetValue(XNode.NodePort port)
    {
        return Array;
    }

    public override void Recalculate()
    {
        int size = GetInputValue<int>("Size", this.Size);
        SizeSquared = size * size;

        Array = new float[SizeSquared];
        Debug.Log("creating new array");

        base.Recalculate();
    }
}

[System.Serializable]
public class RandomNoiseNode : TerrainNode
{
    [Input] public int seed;
    [Input] public bool randomizeSeed;
    [Input] public int MaxNumThreads = 65535;
    [Input] public int numOctaves = 7;
    [Input] public float persistence = .5f;
    [Input] public float lacunarity = 2;
    [Input] public float initialScale = 2;
    [Input] public ComputeShader heightMapComputeShader;
    [Input] public bool GenerationEnabled = true;

    [Input] public float[] BaseHeightMap = new float[128*128];
    [Output] public float[] HeightMap;
    public int HeightMapSize = 128*128;
    public override object GetValue(XNode.NodePort port)
    {
        return HeightMap;
    }

    public override void Recalculate()
    {
        int seed = GetInputValue("seed", this.seed);
        bool randomizeSeed = GetInputValue<bool>("randomizeSeed", this.randomizeSeed);
        int MaxNumThreads = GetInputValue<int>("MaxNumThreads", this.MaxNumThreads);
        int numOctaves = GetInputValue<int>("numOctaves", this.numOctaves);
        float persistence = GetInputValue<float>("persistence", this.persistence);
        float lacunarity = GetInputValue<float>("lacunarity", this.lacunarity);
        float initialScale = GetInputValue<float>("initialScale", this.initialScale);
        ComputeShader heightMapComputeShader = GetInputValue<ComputeShader>("heightMapComputeShader", this.heightMapComputeShader);
        bool GenerationEnabled = GetInputValue<bool>("GenerationEnabled", this.GenerationEnabled);
        float[] BaseHeightMap = GetInputValue<float[]>("BaseHeightMap", this.BaseHeightMap);

        //Recalculate the graph
        RandomNoiseSettings rns = new RandomNoiseSettings()
        {
            seed = seed,
            randomizeSeed = randomizeSeed,
            MaxNumThreads = MaxNumThreads,
            numOctaves = numOctaves,
            persistence = persistence,
            lacunarity = lacunarity,
            initialScale = initialScale,
            heightMapComputeShader = heightMapComputeShader,
            GenerationEnabled = GenerationEnabled,
        };

        RandomNoiseHeightMapLayer rnml = new RandomNoiseHeightMapLayer(rns);

        Debug.Log(BaseHeightMap.Length);
        HeightMap = rnml.Run(BaseHeightMap);
        HeightMapSize = HeightMap.Length;

        base.Recalculate();
    }
}

[System.Serializable]
public class ErosionNode : XNode.Node
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
    [Range(0, 1)]
    [Input] public float inertia = 0.3f;

    [Output] public float[] HeightMap;
}
