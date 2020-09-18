using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateNodeMenu("Operators/Static/Subtraction")]
public class SubtractionNode : BinaryOperatorNode<int>
{
    protected override int DoOperation(int A, int B)
    {
        return A - B;
    }
}