using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateNodeMenu("Visualizers/VisualizeTerrainArrayNode")]
public class VisualizeTerrainArrayNode : VisualizeArrayNode
{
    [Input] public Terrain[] Array;

    private static Dictionary<Terrain, Color> terrainColors = new Dictionary<Terrain, Color>(){
        { Terrain.Empty, Color.yellow },
        { Terrain.Grass, Color.green },
        { Terrain.Mountain, Color.black },
        { Terrain.Water, Color.blue },
    };

    public override void Recalculate()
    {
        Terrain[] Array = GetInputValue("Array", this.Array);

        if (IsInputArrayValid(Array))
        {
            GenerateVisualization(Array, (val) => {
                return terrainColors[val];
            });

            base.Recalculate();
        }
    }
}
