using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System.IO;
using UnityEditor;
using System;

public enum typeOfNoise
{
    Simplex,
    Perlin
}

public enum typeOfFallOff
{
    Circle,
    Rectangle
}

public class GenerateTerrain : MonoBehaviour
{
    Mesh mesh;
    Vector3[] vertices;
    Vector3[] waterVertices;
    int[] triangles;
    Vector2[] uvs;
    float[] minTerrainHeight;
    float[] maxTerrainHeight;
    float[,] heights;
    float minHeight;
    float maxHeight;
    Vector3[] normals;

    [Header("Terrain Settings")]
    public float scale;
    public float elevationScale = 50f;
    public int mapSize = 255;

    public float[,,] heightMap;
    System.Random prng;
    [Header("Terrain fall off Settings")]
    public bool fallOffEnabled = false;
    public typeOfFallOff fallOffType;
    public float a = 10f;
    public float b = 10f;
    public float fallOffHeight = 20f;
    public bool underwaterRavines = true;
    public List<NoiseSettings> noiseLayers;
    public bool erode = true;
    Erosion erosion;
    Texture2D textureHeightMap;
    // Start is called before the first frame update
    void Init()
    {
        if (mesh == null)
        {
            mesh = new Mesh();
            vertices = new Vector3[mapSize * mapSize];
            waterVertices = new Vector3[mapSize * mapSize];
            triangles = new int[(mapSize - 1) * (mapSize - 1) * 6];
            uvs = new Vector2[vertices.Length];
        }

        if (erosion == null)
        {
            erosion = GetComponent<Erosion>();
        }

        heights = new float[mapSize, mapSize];
        heightMap = new float[noiseLayers.Capacity, mapSize, mapSize];
        textureHeightMap = new Texture2D(mapSize, mapSize, TextureFormat.RGBA32, false);
        minTerrainHeight = new float[noiseLayers.Capacity];
        maxTerrainHeight = new float[noiseLayers.Capacity];
        for (int i = 0; i < noiseLayers.Capacity; i++)
        {
            minTerrainHeight[i] = 100;
            maxTerrainHeight[i] = 0;
        }

        minHeight = 100;
        maxHeight = 0;

        GetComponent<MeshFilter>().mesh = mesh;
    }

    void Erode()
    {
        minHeight = 100;
        maxHeight = 0;
        erosion.Erode(ref heights, mapSize, noiseLayers[0].seed);

        for (int z = 0; z < mapSize; z++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                minHeight = Mathf.Min(minHeight, heights[z, x]);
                maxHeight = Mathf.Max(maxHeight, heights[z, x]);
            }
        }

    }
    public void CreateTerrain()
    {
        CreateHeightMap();
        Generate();
        UpdateMesh();
    }
    public void CreateHeightMap()
    {
        Init();
        //Generate height map
        for (int i = 0; i < noiseLayers.Capacity; i++)
        {
            for (int z = 0; z < mapSize; z++)
            {
                for (int x = 0; x < mapSize; x++)
                {
                    heightMap[i, z, x] = Noise(z, x, noiseLayers[i]);
                    minTerrainHeight[i] = Mathf.Min(minTerrainHeight[i], heightMap[i, z, x]);
                    maxTerrainHeight[i] = Mathf.Max(maxTerrainHeight[i], heightMap[i, z, x]);
                }
            }
        }

        //Normalize heights
        for (int i = 0; i < noiseLayers.Capacity; i++)
        {
            for (int z = 0; z < mapSize; z++)
            {
                for (int x = 0; x < mapSize; x++)
                {
                    heightMap[i, z, x] = Mathf.InverseLerp(minTerrainHeight[i], maxTerrainHeight[i], heightMap[i, z, x]);
                    if (noiseLayers[i].useHeightCurve)
                    {
                        heightMap[i, z, x] = noiseLayers[i].heightCurve.Evaluate(heightMap[i, z, x]);
                    }
                }
            }
        }

        for (int z = 0; z < mapSize; z++)
        {
            float zCoord = ((float)z / (mapSize - 1f)) * 2;
            for (int x = 0; x < mapSize; x++)
            {
                float xCoord = ((float)x / (mapSize - 1f)) * 2;
                float height = 0;
                float mask = 1;
                for (int l = 0; l < noiseLayers.Capacity; l++)
                {
                    if (noiseLayers[l].layerActive)
                    {
                        if (l != 0)
                        {
                            mask = noiseLayers[l].useFirstLayerAsMask ? heightMap[0, z, x] : 1;
                        }
                        height += heightMap[l, z, x] * mask;
                    }
                }
                if (fallOffEnabled)
                {
                    height = ApplyFallOff(xCoord, zCoord, height);
                }
                minHeight = Mathf.Min(minHeight, height);
                maxHeight = Mathf.Max(maxHeight, height);
                heights[z, x] = height;
            }
        }

        if (erode)
        {
            Erode();
        }

        for (int z = 0; z < mapSize; z++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                heights[z, x] = Mathf.InverseLerp(minHeight, maxHeight, heights[z, x]);
                textureHeightMap.SetPixel(x, z, new Color(heights[z, x], heights[z, x], heights[z, x]));
            }
        }

        textureHeightMap.Apply();
        byte[] bytes = textureHeightMap.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/Resources/Textures/heightMap.png", bytes);
        AssetDatabase.Refresh();
    }

    public float ApplyFallOff(float x, float z, float height)
    {
        float value = 0;
        float h = 0;
        switch (fallOffType)
        {
            case typeOfFallOff.Circle:
                value = Mathf.Pow(x - 1, 2) + Mathf.Pow(z - 1, 2);
                value /= 2;
                break;
            case typeOfFallOff.Rectangle:
                value = Mathf.Max(Mathf.Abs(x - 1), Mathf.Abs(z - 1));
                break;
        }
        h = Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow(b - b * value, a));
        if (height > fallOffHeight)
        {
            return Mathf.Lerp(height, fallOffHeight, h);
        }
        else
        {
            float clampedHeight;
            if (!underwaterRavines)
            {
                clampedHeight = Mathf.Clamp(height, fallOffHeight, 1);
            }
            else
            {
                clampedHeight = height;
            }
            return Mathf.Lerp(fallOffHeight, clampedHeight, h);
        }

    }

    public void Generate()
    {
        int v = 0;
        int t = 0;
        int i = 0;
        int x = 0;

        if (mapSize > 256)
        {
            Debug.LogError("MapSize exceeds unity's limit of vertices(256).");
            return;
        }

        //Create mesh with the height map
        for (int z = 0; z < mapSize; z++)
        {
            float zCoord = ((float)z / (mapSize - 1f)) * 2;
            for (x = 0; x < mapSize; x++)
            {
                float xCoord = ((float)x / (mapSize - 1f)) * 2;
                vertices[i] = new Vector3((xCoord - 1) * scale, heights[z, x] * elevationScale, (zCoord - 1) * scale);
                waterVertices[i] = new Vector3((xCoord - 1) * scale, 0f, (zCoord - 1) * scale);
                uvs[i] = new Vector2((float)x / mapSize, (float)z / mapSize);
                i++;

                if ((z < mapSize - 1) && (x < mapSize - 1))
                {
                    triangles[t] = v;
                    triangles[t + 1] = v + mapSize;
                    triangles[t + 2] = v + 1;
                    triangles[t + 3] = v + 1;
                    triangles[t + 4] = v + mapSize;
                    triangles[t + 5] = v + mapSize + 1;
                    v++;
                    t += 6;
                }
                if (x == mapSize - 1)
                {
                    v++;
                }
            }
        }
    }

    float Noise(int z, int x, NoiseSettings noiseSettings)
    {
        float tmpAmplitude = noiseSettings.amplitude;
        float tmpFrequency = noiseSettings.frequency;
        float height = 0f;

        prng = new System.Random(noiseSettings.seed);
        float gridSize = (float)mapSize;
        for (int i = 0; i < noiseSettings.octavesSize; i++)
        {
            float xCoord = (float)(x * tmpFrequency / gridSize) + prng.Next(-100000, 100000);
            float zCoord = (float)(z * tmpFrequency / gridSize) + prng.Next(-100000, 100000);
            float value = 0;

            switch (noiseSettings.noiseType)
            {
                case typeOfNoise.Simplex:
                    value = (noise.snoise(new float2(xCoord, zCoord)) * 2) - 1;
                    break;
                case typeOfNoise.Perlin:
                    value = (Mathf.PerlinNoise(xCoord, zCoord) * 2) - 1;
                    break;
            }
            if (noiseSettings.Turbulance)
            {
                value = Mathf.Abs(value);
                if (noiseSettings.Ridges)
                {
                    value = 1 - value;
                    value = Mathf.Pow(value, noiseSettings.RidgesStrength);
                }
            }
            height += value * tmpAmplitude;
            tmpFrequency *= noiseSettings.lacunarity;
            tmpAmplitude *= noiseSettings.persistence;
        }

        return height;
    }

    public void UpdateMesh()
    {
        mesh.Clear();

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        mesh.RecalculateNormals();
    }

    public Texture2D GetTexture()
    {
        return textureHeightMap;
    }

    public float GetMinHeight()
    {
        return minHeight;
    }

    public float GetMaxHeight()
    {
        return maxHeight;
    }

    public Vector3[] GetNormals()
    {
        normals = mesh.normals;
        return normals;
    }

    public Vector3[] GetWaterVertices()
    {
        return waterVertices;
    }

    public int[] GetWaterTriangles()
    {
        return triangles;
    }

    public Vector2[] GetWaterUVS()
    {
        return uvs;
    }

}
