using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateNodeMenu("Operators/Static/Division")]
public class DivisionNode : BinaryOperatorNode<int>
{
    protected override int DoOperation(int A, int B)
    {
        return A / B;
    }
}
