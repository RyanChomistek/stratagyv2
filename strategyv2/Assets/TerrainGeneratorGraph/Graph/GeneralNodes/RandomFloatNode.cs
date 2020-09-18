using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomFloatNode : RandomValueNode<float>
{
    public override float GetValue(float min, float max)
    {
        if (Graph.Rand != null)
        {
            return (float) ((Graph.Rand.NextDouble() * max) - min);
        }

        return -1;
    }
}

