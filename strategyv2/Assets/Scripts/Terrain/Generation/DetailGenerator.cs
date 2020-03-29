using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetailGenerator : MonoBehaviour
{
    [SerializeField]
    private Mesh m_DetailMesh;

    [SerializeField]
    private Material m_Material;

    [Tooltip("The density of this detail, 1 means 1 per tile, 2 2 per 2 tile, .5 .5 per tile")]
    private float Density;

    public void GenerateDetailLayer(TerrainData tData, MapData mapData, MeshGeneratorArgs otherArgs, int layer)
    {
    }
}
