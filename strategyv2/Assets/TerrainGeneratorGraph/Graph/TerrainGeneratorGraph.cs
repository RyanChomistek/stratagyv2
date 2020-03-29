using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[CreateAssetMenu(fileName = "NewGraph", menuName = "settings/TerrainGraph", order = 0)]
public class TerrainGeneratorGraph : NodeGraph
{
    public bool PauseRecalculation = false;
    public bool RandomizeSeed = false;

    [SerializeField]
    private int seed = 0;
    public System.Random Rand;


    public void RecalculateFullGraph()
    {
        if(!PauseRecalculation)
        {
            RecalculateFullGraphNoFlush();
            FlushNodeData();
        }
    }

    public void ForceRecalculateFullGraph()
    {
        RecalculateFullGraphNoFlush();
        FlushNodeData();
    }

    // This will recalculate the whole graph via back propogation
    private void RecalculateFullGraphNoFlush()
    {
        bool oldPauseRecalculationState = PauseRecalculation;
        PauseRecalculation = false;

        int randomSeed = (RandomizeSeed) ? new System.Random((int) System.DateTime.Now.Ticks).Next(-10000, 10000): seed;

        Debug.Log($"SEED {randomSeed}, {seed}, {RandomizeSeed}");

        Rand = new System.Random(randomSeed);

        List<SelfPropagatingNode> roots = FindRoots();

        HashSet<SelfPropagatingNode> calcedNodes = new HashSet<SelfPropagatingNode>();
        foreach(var root in roots)
        {
            BackPropogate(root, calcedNodes);
        }

        PauseRecalculation = oldPauseRecalculationState;
    }

    public List<SelfPropagatingNode> FindRoots()
    {
        List<SelfPropagatingNode> roots = new List<SelfPropagatingNode>();

        int i = 0;

        // Find the mesh generation node
        foreach (var node in nodes)
        {
            i++;
            SelfPropagatingNode SPN = null;
            if ((SPN = node as SelfPropagatingNode) && FindOutputNodes(SPN).Count == 0)
            {
                roots.Add(SPN);
            }
        }

        return roots;
    }

    public MapData RecalculateFullGraphAndGetMapData()
    {
        RecalculateFullGraphNoFlush();

        MapData md = FindMeshNode().GetValueAsMapData();

        FlushNodeData();

        return md;
    }

    public MeshGenerationNode FindMeshNode()
    {
        MeshGenerationNode root = null;

        // Find the mesh generation node
        foreach (var node in nodes)
        {
            if (root = node as MeshGenerationNode)
            {
                break;
            }
        }

        if (root == null)
        {
            throw new System.Exception("Failed to find mesh Generation Node");
        }

        return root;
    }

    private void BackPropogate(SelfPropagatingNode current, HashSet<SelfPropagatingNode> calcedNodes)
    {
        HashSet<SelfPropagatingNode> inputs = FindInputNodes(current);
        
        foreach (SelfPropagatingNode input in inputs)
        {
            if(!calcedNodes.Contains(input))
            {
                BackPropogate(input, calcedNodes);
                calcedNodes.Add(input);
            }
        }

        current.IsError = false;
        try
        {
            current.Recalculate();
        }
        catch(Exception e)
        {
            current.IsError = true;
        }
        //Debug.Log(current);
    }

    private void FlushNodeData()
    {
        foreach (var node in nodes)
        {
            SelfPropagatingNode SPN = null;
            if ((SPN = node as SelfPropagatingNode))
            {
                SPN.Flush();
            }
        }
    }

    private HashSet<SelfPropagatingNode> FindInputNodes(SelfPropagatingNode node)
    {
        HashSet<SelfPropagatingNode> inputs = new HashSet<SelfPropagatingNode>();
        foreach (var port in node.Ports)
        {
            if (port.IsInput)
            {
                foreach (NodePort otherInputPort in port.GetConnections())
                {
                    SelfPropagatingNode otherNode = null;
                    if((otherNode = otherInputPort.node as SelfPropagatingNode) && !inputs.Contains(otherNode))
                    {
                        inputs.Add(otherNode);
                    }
                }
            }
        }

        return inputs;
    }

    private HashSet<SelfPropagatingNode> FindOutputNodes(SelfPropagatingNode node)
    {
        HashSet<SelfPropagatingNode> outputs = new HashSet<SelfPropagatingNode>();
        foreach (var port in node.Ports)
        {
            if (port.IsOutput)
            {
                foreach (NodePort otherInputPort in port.GetConnections())
                {
                    SelfPropagatingNode otherNode = null;
                    if ((otherNode = otherInputPort.node as SelfPropagatingNode) && !outputs.Contains(otherNode))
                    {
                        outputs.Add(otherNode);
                    }
                }
            }
        }

        return outputs;
    }
}
