using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WallRuleTile : RuleTile
{
    protected override void GetMatchingNeighboringTiles(ITilemap tilemap, Vector3Int position, ref TileBase[] neighboringTiles)
    {
        if (neighboringTiles != null)
            return;
        
        if (m_CachedNeighboringTiles == null || m_CachedNeighboringTiles.Length < neighborCount)
            m_CachedNeighboringTiles = new TileBase[neighborCount];

        //search for connected component of the same type
        int index = 0;
        for (int y = 1; y >= -1; y--)
        {
            for (int x = -1; x <= 1; x++)
            {
                if (x != 0 || y != 0)
                {
                    Vector3Int tilePosition = new Vector3Int(position.x + x, position.y + y, position.z);
                    m_CachedNeighboringTiles[index++] = tilemap.GetTile(tilePosition);
                }
            }
        }
        neighboringTiles = m_CachedNeighboringTiles;
    }

    private List<TileBase> FindEdge(Vector3Int position, ref HashSet<TileBase> edge)
    {
        return null;
    }

}
