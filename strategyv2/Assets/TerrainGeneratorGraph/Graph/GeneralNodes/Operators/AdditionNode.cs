using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateNodeMenu("Operators/Static/Addition")]
public class AdditionNode : BinaryOperatorNode<int>
{
    protected override int DoOperation(int A, int B)
    {
        return A + B;
    }
}
