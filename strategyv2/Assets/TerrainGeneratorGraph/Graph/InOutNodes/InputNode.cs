using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateNodeMenu("")]
public abstract class InputNode : TerrainNode
{
    // We will get all of our data from this source
    private SubGraphNode Source;

    public void SetSource(SubGraphNode source)
    {
        Source = source;
    }

    private T GetInputValueFromSource<T>(string fieldName, T fallback = default)
    {
        return Source.GetInputValue(fieldName, fallback);
    }

    protected override void SetLocals(System.Type hostType)
    {
        foreach (var field in hostType.GetFields())
        {
            if (hostType != field.DeclaringType)
            {
                continue;
            }

            object value = field.GetValue(this);
            field.SetValue(this, GetInputValueFromSource(field.Name, value));
        }
    }

    public override void Flush()
    {
        Source = null;
    }

    public override void Recalculate()
    {
        throw new System.NotImplementedException();
    }
}
