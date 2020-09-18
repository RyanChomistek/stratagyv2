using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomIntNode : RandomValueNode<int>
{
    public override int GetValue(int Min, int Max)
    {
        if(Graph.Rand != null)
        {
            return Graph.Rand.Next(Min, Max);
        }

        return -1;
    }
}
