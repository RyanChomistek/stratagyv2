using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BinaryOperatorNode<T> : SelfPropagatingNode
{
    [Input] public T A;
    [Input] public T B;

    [Output] public T Output;

    public override object GetValue(XNode.NodePort port)
    {
        if (port.fieldName == "Output")
            return Output;

        return null;
    }

    public override void Flush()
    {
    }

    public override void Recalculate()
    {
        T A = GetInputValue<T>("A", this.A);
        T B = GetInputValue<T>("B", this.B);

        Output = DoOperation(A, B);
    }

    protected abstract T DoOperation(T A, T B);
}
