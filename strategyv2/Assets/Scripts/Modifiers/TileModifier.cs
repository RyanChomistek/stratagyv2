using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileModifier : DivisionModifier
{
    private string _name = "-1";

    public override DivisionModifier ModifyDivision(Division divisionToModify)
    {
        base.ModifyDivision(divisionToModify);

        var pos = divisionToModify.Position;
        if (MapManager.Instance != null)
        {
            var tile = MapManager.Instance.GetTileFromPosition(pos);
            _name = tile.TerrainType.ToString();
            var prespeed = divisionToModify.Speed;
            divisionToModify.Speed *= 1 / (float)tile.MoveCost;
        }

        return this;
    }

    public override string ToString()
    {
        return _name;
    }
}
