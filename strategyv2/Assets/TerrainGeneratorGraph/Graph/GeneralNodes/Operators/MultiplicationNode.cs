using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateNodeMenu("Operators/Static/Multiplication")]
public class MultiplicationNode : BinaryOperatorNode<int>
{
    protected override int DoOperation(int A, int B)
    {
        return A * B;
    }
}
