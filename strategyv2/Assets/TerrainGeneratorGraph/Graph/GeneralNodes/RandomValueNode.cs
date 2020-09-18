using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class RandomValueNode<T> : XNode.Node
{
    protected TerrainGeneratorGraph Graph { get { return graph as TerrainGeneratorGraph; } }

    [Input] public T Min;
    [Input] public T Max;

    [Output] public T Value;

    public T LastUsedValue;
    public override object GetValue(XNode.NodePort port)
    {
        if (port.fieldName == "Value")
        {
            T Min = GetInputValue("Min", this.Min);
            T Max = GetInputValue("Max", this.Max);
            LastUsedValue = GetValue(Min, Max);
            
            return LastUsedValue;
        }

        return null;
    }

    public abstract T GetValue(T Min, T Max);
}

