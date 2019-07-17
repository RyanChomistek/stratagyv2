using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "NewTerrainTile", menuName = "TerrainTile", order = 0)]
public class TerrainTileSettings : ScriptableObject
{
    [SerializeField]
    public MapTerrainTile tile;
}
