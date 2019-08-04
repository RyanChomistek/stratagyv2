using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ITickingModifier
{
    void OnTick(Division division);
}
