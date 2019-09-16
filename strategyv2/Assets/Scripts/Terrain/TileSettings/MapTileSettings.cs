using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "MapTile", menuName = "MapTile", order = 0)]
public class MapTileSettings : ScriptableObject
{
    public MapLayer Layer;

    // Will only ever use one of these
    [SerializeField]
    public TerrainMapTile TerrainMapTileSettings;
    [SerializeField]
    public ImprovementMapTile ImprovementMapTileSettings;
}
