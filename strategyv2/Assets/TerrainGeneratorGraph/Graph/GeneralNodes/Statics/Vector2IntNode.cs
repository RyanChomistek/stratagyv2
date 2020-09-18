using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateNodeMenu("StaticNodes/Vector2Int")]
public class Vector2IntNode : XNode.Node
{
    [Input] public int X = 0;
    [Input] public int Y = 0;

    [Output] public Vector2Int Output;

    public override object GetValue(XNode.NodePort port)
    {
        if (port.fieldName == "Output")
        {
            int X = GetInputValue("X", this.X);
            int Y = GetInputValue("Y", this.Y);

            return new Vector2Int(X, Y); ;
        }

        return null;
    }
}
