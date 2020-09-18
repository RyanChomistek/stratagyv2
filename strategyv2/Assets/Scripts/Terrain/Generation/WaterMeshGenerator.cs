using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterMeshGenerator : MonoBehaviour
{
    [SerializeField]
    private GameObject m_WaterPlane = null;
    [SerializeField]
    private GameObject m_WaterTilePrefab;


    [SerializeField]
    private GameObject m_WaterMeshPrefab;
    [SerializeField]
    private List<GameObject> m_WaterMeshes = new List<GameObject>();

    public void ConstructWaterPlaneMesh(MapData mapData, MeshGeneratorArgs meshArgs)
    {
        // Clear any old water meshes
        m_WaterMeshes.ForEach(x => DestroyImmediate(x));
        m_WaterMeshes.Clear();

        if (!meshArgs.GenerateWaterMeshes)
        {
            return;
        }

        //m_WaterPlane.transform.localScale = new Vector3(m_Terrain.terrainData.bounds.max.x, m_Terrain.terrainData.bounds.max.x, m_Terrain.terrainData.bounds.max.x) / 100;
        UnityStandardAssets.Water.PlanarReflection reflection = m_WaterPlane.GetComponent<UnityStandardAssets.Water.PlanarReflection>();
        UnityStandardAssets.Water.WaterBase waterBase = m_WaterPlane.GetComponent<UnityStandardAssets.Water.WaterBase>();

        m_WaterPlane.transform.position = Vector3.zero;

        int waterTileSize = 50;
        for (int x = 0; x < meshArgs.MeshSize.x; x += waterTileSize)
        {
            for (int z = 0; z < meshArgs.MeshSize.z; z += waterTileSize)
            {
                UnityStandardAssets.Water.WaterTile waterTile = GameObject.Instantiate(m_WaterTilePrefab).GetComponent<UnityStandardAssets.Water.WaterTile>();
                waterTile.reflection = reflection;
                waterTile.waterBase = waterBase;

                //m_Terrain.terrainData.bounds.max.y
                waterTile.transform.parent = waterBase.transform;
                waterTile.transform.localPosition = new Vector3(x + waterTileSize / 2, 0, z + waterTileSize / 2);

                m_WaterMeshes.Add(waterTile.gameObject);
            }
        }

        m_WaterPlane.transform.position = new Vector3(0, mapData.WaterHeight * (meshArgs.MeshSize.y), 0);
    }
}
