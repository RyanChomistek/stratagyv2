using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[CreateNodeMenu("")]
public abstract class SelfPropagatingNode : XNode.Node
{
    protected TerrainGeneratorGraph Graph { get { return base.graph as TerrainGeneratorGraph; } }

    public bool IsError = false;

    // should be called on the first node to set of a recalc chain
    public void StartPropogation()
    {
        (Graph as TerrainGeneratorGraph).RecalculateFullGraph();
    }

    // Starts a recalculation using this node as the source
    public void Propogate()
    {
        // Need to topologically sort nodes so we dont trigger recalcs on a single node more than once/before its dependancies have been recalced
        System.DateTime FindingDependenciesStart = System.DateTime.Now;

        // find all the ports on this node that are output
        List<NodePort> outputPorts = new List<NodePort>();
        foreach (var port in this.Ports)
        {
            if (port.IsOutput)
            {
                outputPorts.Add(port);
            }
        }

        // Find all of the dependancies on the nodes we have outputs to
        Dictionary<Node, HashSet<Node>> NodeDependancies = new Dictionary<Node, HashSet<Node>>();
        for (int i = 0; i < outputPorts.Count; i++)
        {
            NodePort port = outputPorts[i];
            foreach (NodePort otherInputPort in port.GetConnections())
            {
                HashSet<Node> ReachableNodes = new HashSet<Node>();
                // Find all reachable nodes of from this one
                if (GetAllReachableNodes(otherInputPort.node, ReachableNodes))
                {
                    NodeDependancies[otherInputPort.node] = ReachableNodes;
                }
                else
                {
                    // we hit a cycle abort
                    return;
                }
            }
        }

        System.DateTime FindingDependenciesEnd = System.DateTime.Now;
        System.DateTime RunningChildrenStart = System.DateTime.Now;

        // Find nodes that are not in any of the dependancies of other nodes and recalculate them
        List<SelfPropagatingNode> nodesWithNoConflictingDependencies = new List<SelfPropagatingNode>();
        foreach (var nodeListPair in NodeDependancies)
        {
            Node node = nodeListPair.Key;
            HashSet<Node> reachableNodes = nodeListPair.Value;

            bool notInAnyOtherNodesDependancies = true;

            foreach (var otherNodeListPair in NodeDependancies)
            {
                Node otherNode = otherNodeListPair.Key;
                HashSet<Node> otherReachableNodes = otherNodeListPair.Value;
                if (otherReachableNodes.Contains(node))
                {
                    notInAnyOtherNodesDependancies = false;
                    break;
                }
            }

            if (notInAnyOtherNodesDependancies)
            {
                // need to make only calculate the next node not keep going down the chain!
                SelfPropagatingNode propogatingNode;
                if (propogatingNode = node as SelfPropagatingNode)
                {
                    propogatingNode.EnterRecalculate();
                    nodesWithNoConflictingDependencies.Add(propogatingNode);
                }
            }
        }

        foreach (var node in nodesWithNoConflictingDependencies)
        {
            RecalculateNextNode(node);
        }

        System.DateTime RunningChildrenEnd = System.DateTime.Now;

        TimeSpan FindingDependenciesTime = FindingDependenciesEnd - FindingDependenciesStart;
        TimeSpan RunningChildrenTime = RunningChildrenEnd - RunningChildrenStart;

        
        Debug.Log($"{this.name} time spent finding dependencies: {FindingDependenciesTime}, Time Spent Recalculating Children {RunningChildrenTime}");
    }

    private static bool GetAllReachableNodes(Node currentNode, HashSet<Node> reachableNodes)
    {
        foreach (NodePort outputPort in currentNode.Ports)
        {
            if (outputPort.IsOutput)
            {
                foreach (NodePort otherInputPort in outputPort.GetConnections())
                {
                    if (reachableNodes.Contains(otherInputPort.node))
                    {
                        Debug.LogError($"CYCLE DETECTED IN GRAPH | root node: {otherInputPort.node.name}");
                        return false;
                    }

                    reachableNodes.Add(otherInputPort.node);

                    // Find all reachable nodes of from this one
                    return GetAllReachableNodes(otherInputPort.node, reachableNodes);
                }
            }
        }

        return true;
    }

    public void EnterRecalculate()
    {
        TimeSpan recalcTime = TimeSpan.Zero;
        System.DateTime startRecalc = System.DateTime.Now;
        if (!(this.Graph as TerrainGeneratorGraph).PauseRecalculation)
        {
            IsError = false;
            this.Recalculate();
            System.DateTime endRecalc = System.DateTime.Now;
            recalcTime = endRecalc - startRecalc;
            Debug.Log($"{this.name} time spent in recalc: {recalcTime}");
        }
    }

    public abstract void Recalculate();

    public abstract void Flush();

    /// <summary>
    /// this function is used to recalc the next node in the calc chain, you might want to pass some information to that next node
    /// so do it here
    /// </summary>
    /// <param name="propogatingNode"> the next node in the calc chain </param>
    public virtual void RecalculateNextNode(SelfPropagatingNode propogatingNode)
    {
        propogatingNode.Propogate();
    }

    public override void OnCreateConnection(NodePort from, NodePort to)
    {
       // StartPropogation();
    }

    public override void OnRemoveConnection(NodePort port)
    {
        //StartPropogation();
    }

    public void GetInputs<T>(System.Type hostType)
    {
        foreach (var field in this.GetType().GetFields())
        {
            if (hostType != field.DeclaringType)
            {
                continue;
            }

            object value = field.GetValue(this);
            field.SetValue(this, GetInputValue(field.Name, value));
        }
    }

    public void FlushInputs<T>(System.Type hostType)
    {
        foreach (var field in this.GetType().GetFields())
        {
            if (hostType != field.DeclaringType)
            {
                continue;
            }

            object value = field.GetValue(this);
            field.SetValue(this, null);
        }
    }

    public bool IsInputArrayValid<T>(T[] arr)
    {
        return arr != null && arr.Length > 0;
    }

    protected bool AreAllValid<T>(params T[][] list)
    {
        foreach(var obj in list)
        {
            if(obj == null || obj.Length == 0)
            {
                return false;
            }
        }

        return true;
    }
}