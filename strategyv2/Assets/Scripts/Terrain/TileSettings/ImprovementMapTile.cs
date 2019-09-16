using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ImprovementMapTile : MapTile
{
    // Right now this is just a stub but will have unique stuff later

    public Improvement Improvement;

    public ImprovementMapTile(ImprovementMapTile other)
        : base(other)
    {
        this.Improvement = other.Improvement;
    }
}
