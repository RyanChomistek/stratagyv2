using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapHighlight : MonoBehaviour
{
    private MapManager _map;
    Dictionary<Vector2Int, Color> returnColors;
    public Color HighlightColor = Color.cyan;
    
    public TileBase BlankTile;
    [SerializeField]
    private Tilemap _highlightTileMap;
    // Start is called before the first frame update
    void Start()
    {
        returnColors = new Dictionary<Vector2Int, Color>();
        HoverHandler handler = new HoverHandler((x, y) => { HighlightTile(y); }, 
            (x, y) => {
                var prevMousePosition = y;
                var currMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                //dont return the tile if the cursor is still on the same tile
                if (_map.GetTilePositionFromPosition(prevMousePosition) != _map.GetTilePositionFromPosition(currMousePosition))
                {
                    ReturnTile(y);
                }
            },
            (x,y) => {  }, (x,y) => { }, (x,y) => { });

        InputController.Instance.RegisterHoverHandler(handler);
        _map = MapManager.Instance;
        _map.RegisterOnMapRerenderCallback(OnMapRerender);
    }

    //after the map rerenders make sure that we recolor highlight tiles
    private void OnMapRerender()
    {
        /*
        var keys = returnColors.Keys.ToList();
        foreach (var key in keys)
        {
            //the color of the tile may have changed
            //returnColors[key] = _map.GetTileColor(key);
            _map.SetTileColor(key, HighlightColor);
        }
        */
    }

    private void HighlightTile(Vector3 position)
    {
        
        var tilePosition = _map.GetTilePositionFromPosition(position);

        if(!returnColors.ContainsKey(tilePosition) && MapManager.InBounds(_map.map, tilePosition.x, tilePosition.y))
        {
            returnColors.Add(tilePosition, _map.GetTileColor(tilePosition));
            _highlightTileMap.SetTile(new Vector3Int(tilePosition.x, tilePosition.y, 0), BlankTile);
        }
    }

    private void ReturnTile(Vector3 position)
    {
        var tilePosition = _map.GetTilePositionFromPosition(position);
        if(returnColors.TryGetValue(tilePosition, out Color color))
        {
            returnColors.Remove(tilePosition);
            _highlightTileMap.SetTile(new Vector3Int(tilePosition.x, tilePosition.y, 0), null);
        }
    }
}
