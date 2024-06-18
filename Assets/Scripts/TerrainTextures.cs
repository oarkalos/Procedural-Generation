using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows;
using UnityEditor;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;

public class TerrainTextures : MonoBehaviour
{
    public Texture2D[] albedo;
    public Texture2D[] normal;
    public Texture2D[] mask;
    [HideInInspector]
    public Texture2D rockAlbedo;
    [HideInInspector]
    public Texture2D rockNormal;
    [HideInInspector]
    public Texture2D rockMask;
    [HideInInspector]
    public Material material; // Material with shader attached.
    ComputeShader textureTerrain;
    [HideInInspector]
    public int textureResolution = 2048;
    [HideInInspector]
    public float2 tiling = new float2(1, 1);
    [HideInInspector]
    public float2 offset = new float2(0, 0);
    [HideInInspector]
    [Header("Height of Textures")]
    public float heightOfSnow = 0.8f;
    [HideInInspector]
    public float heightOfGrass = 0.4f;
    [HideInInspector]
    public float depth = 0.9f;
    [HideInInspector]
    public float heightOfBlend = 0.5f;
    [HideInInspector]
    [Header("Slope texture blending")]
    public float slope = 0.5f;
    [HideInInspector]
    public float blendAmount = 0.5f;

    Texture2DArray CreateTextureArray(Texture2D[] textures, bool isLinear)
    {
        Texture2DArray array = new Texture2DArray(textures[0].width, textures[0].height, textures.Length, TextureFormat.RGBA32, isLinear);
        array.filterMode = FilterMode.Bilinear;
        array.wrapMode = TextureWrapMode.Repeat;
        for (int i = 0; i < textures.Length; i++)
        {
            array.SetPixels(textures[i].GetPixels(), i, 0);
        }
        array.Apply();
        return array;
    }

    void DispatchShader()
    {
        float4[,] tmpMap = new float4[textureResolution, textureResolution];
        for (int i = 0; i < textureResolution; i++)
        {
            for (int j = 0; j < textureResolution; j++)
            {
                tmpMap[i, j] = new float4(0, 0, 0, 0);
            }
        }
        ComputeBuffer albedoBuffer = new ComputeBuffer(tmpMap.Length, sizeof(float) * 4);
        albedoBuffer.SetData(tmpMap);
        textureTerrain.SetBuffer(0, "albedoResult", albedoBuffer);

        ComputeBuffer normalBuffer = new ComputeBuffer(tmpMap.Length, sizeof(float) * 4);
        normalBuffer.SetData(tmpMap);
        textureTerrain.SetBuffer(0, "normalResult", normalBuffer);

        ComputeBuffer maskBuffer = new ComputeBuffer(tmpMap.Length, sizeof(float) * 4);
        maskBuffer.SetData(tmpMap);
        textureTerrain.SetBuffer(0, "maskResult", maskBuffer);

        Vector3[] normals = GetComponent<GenerateTerrain>().GetNormals();
        int mapSize = GetComponent<GenerateTerrain>().mapSize;
        Texture2D normalsTex = new Texture2D(mapSize, mapSize, TextureFormat.RGBA32, false);
        int f = 0;
        for (int j = 0; j < mapSize; j++)
        {
            for (int k = 0; k < mapSize; k++)
            {
                normalsTex.SetPixel(k, j, new Color(normals[f].x * 0.5f + 0.5f, normals[f].y * 0.5f + 0.5f, normals[f].z * 0.5f + 0.5f, 1));
                f++;
            }
        }
        normalsTex.Apply();

        textureTerrain.SetTexture(0, "normals", normalsTex);
        textureTerrain.SetTexture(0, "albedoTextures", CreateTextureArray(albedo, false));
        textureTerrain.SetTexture(0, "normalTextures", CreateTextureArray(normal, false));
        textureTerrain.SetTexture(0, "maskTextures", CreateTextureArray(mask, true));
        textureTerrain.SetTexture(0, "heightMap", GetComponent<GenerateTerrain>().GetTexture());
        textureTerrain.SetTexture(0, "RockAlbedo", rockAlbedo);
        textureTerrain.SetTexture(0, "RockNormal", rockNormal);
        textureTerrain.SetTexture(0, "RockMask", rockMask);

        textureTerrain.SetInt("textureResolution", textureResolution);
        textureTerrain.SetFloat("heightOfSnow", heightOfSnow);
        textureTerrain.SetFloat("heightOfGrass", heightOfGrass);
        textureTerrain.SetFloat("depth", depth);
        textureTerrain.SetFloat("heightOfBlend", heightOfBlend);
        textureTerrain.SetFloat("slopeFactor", slope);
        textureTerrain.SetFloat("blendAmount", blendAmount);
        textureTerrain.SetVector("tilingAndOffset", new Vector4(tiling.x, tiling.y, offset.x, offset.y));

        textureTerrain.Dispatch(0, textureResolution / 16, textureResolution / 16, 1);
        albedoBuffer.GetData(tmpMap);

        Texture2D albedoResult = new Texture2D(textureResolution, textureResolution, TextureFormat.RGBA32, false);
        albedoResult.filterMode = FilterMode.Bilinear;
        albedoResult.wrapMode = TextureWrapMode.Repeat;
        for (int i = 0; i < textureResolution; i++)
        {
            for (int j = 0; j < textureResolution; j++)
            {
                albedoResult.SetPixel(j, i, new Color(tmpMap[i, j].x, tmpMap[i, j].y, tmpMap[i, j].z, tmpMap[i, j].w));
            }
        }
        albedoResult.Apply();
        normalBuffer.GetData(tmpMap);

        Texture2D normalResult = new Texture2D(textureResolution, textureResolution, TextureFormat.RGBA32, true);
        normalResult.filterMode = FilterMode.Bilinear;
        normalResult.wrapMode = TextureWrapMode.Repeat;
        for (int i = 0; i < textureResolution; i++)
        {
            for (int j = 0; j < textureResolution; j++)
            {
                normalResult.SetPixel(j, i, new Color(tmpMap[i, j].x * 0.5f + 0.5f, tmpMap[i, j].y * 0.5f + 0.5f, tmpMap[i, j].z * 0.5f + 0.5f, tmpMap[i, j].w));
            }
        }
        normalResult.Apply();
        maskBuffer.GetData(tmpMap);

        Texture2D maskesult = new Texture2D(textureResolution, textureResolution, TextureFormat.RGBA32, false);
        maskesult.filterMode = FilterMode.Bilinear;
        maskesult.wrapMode = TextureWrapMode.Repeat;
        for (int i = 0; i < textureResolution; i++)
        {
            for (int j = 0; j < textureResolution; j++)
            {
                maskesult.SetPixel(j, i, new Color(tmpMap[i, j].x, tmpMap[i, j].y, tmpMap[i, j].z, tmpMap[i, j].w));
            }
        }
        maskesult.Apply();

        byte[] bytes = albedoResult.EncodeToPNG();
        System.IO.File.WriteAllBytes("Assets/Resources/Textures/TerrainAlbedo.png", bytes);
        bytes = normalResult.EncodeToPNG();
        System.IO.File.WriteAllBytes("Assets/Resources/Textures/TerrainNormal.png", bytes);
        bytes = maskesult.EncodeToPNG();
        System.IO.File.WriteAllBytes("Assets/Resources/Textures/TerrainMask.png", bytes);
        AssetDatabase.Refresh();

        albedoBuffer.Release();
        normalBuffer.Release();
        maskBuffer.Release();
    }

    void WriteTextures()
    {
        material.SetTexture("_AlbedoTextures", CreateTextureArray(albedo, false));
        material.SetTexture("_NormalTextures", CreateTextureArray(normal, false));
        material.SetTexture("_MaskTextures", CreateTextureArray(mask, false));
        RenderTexture mRt = new RenderTexture(textureResolution, textureResolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        Graphics.Blit(null, mRt, material, -1); // Applies Shader to buffer.
        RenderTexture.active = mRt; // Sets buffer as render target.
        Texture2D destination = new Texture2D(textureResolution, textureResolution, TextureFormat.RGBA32, false); // creates Texture2D for result.
        destination.ReadPixels(new Rect(0, 0, textureResolution, textureResolution), 0, 0, false); // Reads pixels from current render target
        System.IO.File.WriteAllBytes("Assets/Resources/Textures/TerrainAlbedo.png", destination.EncodeToPNG()); // Writes texture as png to the desired location.
        AssetDatabase.Refresh();

        RenderTexture.active = null;
        mRt.Release();
    }

    void OnValidate()
    {
        if (textureTerrain == null)
        {
            textureTerrain = (ComputeShader)Resources.Load("Shaders/TextureTerrain");
        }
        //WriteTextures();
        //DispatchShader();
        /*GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_BaseColorMap", (Texture2D)Resources.Load("Textures/TerrainAlbedo"));
        GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_NormalMapOS", (Texture2D)Resources.Load("Textures/TerrainNormal"));
        GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_MaskMap", (Texture2D)Resources.Load("Textures/TerrainMask"));*/
        GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_AlbedoTextures", CreateTextureArray(albedo, false));
        GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_NormalTextures", CreateTextureArray(normal, true));
        GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_MaskTextures", CreateTextureArray(mask, false));
    }
}
