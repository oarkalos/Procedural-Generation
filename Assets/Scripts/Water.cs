using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Rendering;

public class Water : MonoBehaviour
{
    public bool enableWater = false;
    public float waterHeight = 0.1f;
    GenerateTerrain generateTerrain;
    GameObject water;
    Texture2D waterHeightMap;
    Mesh mesh;

    IEnumerator Destroy(GameObject obj)
    {
        yield return null;
        DestroyImmediate(obj);
    }

    void OnValidate()
    {

        if (generateTerrain == null)
        {
            generateTerrain = GetComponent<GenerateTerrain>();
        }
        if (waterHeightMap == null)
        {
            waterHeightMap = new Texture2D(generateTerrain.mapSize, generateTerrain.mapSize, TextureFormat.RGBA32, false);
        }

        if (enableWater)
        {
            if (water == null)
            {
                //water = GameObject.CreatePrimitive(PrimitiveType.Plane);
                water = new GameObject("Water");
                water.AddComponent<MeshFilter>();
                water.AddComponent<MeshRenderer>();
                mesh = new Mesh();
                mesh.Clear();
                mesh.vertices = generateTerrain.GetWaterVertices();
                mesh.triangles = generateTerrain.GetWaterTriangles();
                mesh.uv = generateTerrain.GetWaterUVS();
                mesh.RecalculateNormals();
                //water.GetComponent<MeshCollider>().enabled = false;
                water.transform.parent = generateTerrain.transform;
                water.transform.localScale = new Vector3(1f, 1f, 1f);
                water.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                water.name = "Water";
                Material newMat = Resources.Load("Water", typeof(Material)) as Material;
                water.GetComponent<MeshRenderer>().material = newMat;
                water.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                water.GetComponent<MeshFilter>().mesh = mesh;
            }
            water.transform.position = new Vector3(water.transform.parent.position.x, water.transform.parent.position.y + waterHeight,
                                water.transform.parent.position.z);
        }
        else
        {
            if (water != null)
            {
                StartCoroutine(Destroy(water));
            }
        }

    }
}
