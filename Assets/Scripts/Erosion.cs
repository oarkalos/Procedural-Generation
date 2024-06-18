using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using Unity.Mathematics;
using UnityEngine;
using System.IO;
using UnityEditor;

public class Erosion : MonoBehaviour
{
    [Header("Erosion settings")]
    public int erosionIterations;
    public int brushRadius;
    public int maxLifeTime;
    public int erosionTrackCounter;
    [Range(0, 1)]
    public float erosionMaskHeight;
    [Range(0, 1)]
    public float inertia;
    public float capacity;
    public float minSlope;
    [Range(0, 1)]
    public float deposition;
    [Range(0, 1)]
    public float erosionSpeed;
    [Range(0, 1)]
    public float evaporateSpeed;
    public float gravity;
    public float startSpeed;
    public float startWater;
    System.Random prng;
    Texture2D erosionMask;
    int mapSize;

    Vector3 CalculateNewDirection(float4 gradients, float u, float v, Vector3 dir)
    {
        float gradientX = (gradients.y - gradients.x) * (1 - v) + (gradients.w - gradients.z) * v;
        float gradientY = (gradients.z - gradients.x) * (1 - u) + (gradients.w - gradients.y) * u;

        dir.x = dir.x * inertia - gradientX * (1 - inertia);
        dir.y = dir.y * inertia - gradientY * (1 - inertia);
        return dir.normalized;
    }

    public void Erode(ref float[,] heightMap, int mapSize, int noiseLayerSeed)
    {
        this.mapSize = mapSize;
        // Generate random indices for droplet placement
        prng = new System.Random(noiseLayerSeed);
        if (!erosionMask)
        {
            erosionMask = new Texture2D(mapSize, mapSize, TextureFormat.RGBA32, false);
        }

        for (int j = 0; j < mapSize; j++)
        {
            for (int k = 0; k < mapSize; k++)
            {
                erosionMask.SetPixel(k, j, new Color(0, 0, 0, 0));
            }
        }

        // Create brush
        List<int2> brushIndexOffsets = new List<int2>();
        List<float> brushWeights = new List<float>();

        float weightSum = 0;
        for (int brushY = -brushRadius; brushY <= brushRadius; brushY++)
        {
            for (int brushX = -brushRadius; brushX <= brushRadius; brushX++)
            {
                float sqrDst = brushX * brushX + brushY * brushY;
                if (sqrDst < brushRadius * brushRadius)
                {
                    brushIndexOffsets.Add(new int2(brushX, brushY));
                    float brushWeight = brushRadius - Mathf.Sqrt(sqrDst);
                    weightSum += brushWeight;
                    brushWeights.Add(brushWeight);
                }
            }
        }
        for (int j = 0; j < brushWeights.Count; j++)
        {
            brushWeights[j] /= weightSum;
        }

        int tmpTrackCounter = erosionTrackCounter;
        for (int j = 0; j < erosionIterations; j++)
        {
            float2 position = new float2(prng.Next(0, mapSize - 1), prng.Next(0, mapSize - 1));
            float speed = startSpeed;
            Vector3 direction = new Vector3(0, 0, 0);
            float sediment = 0;
            float water = startWater;
            float4 gradients = new float4(0, 0, 0, 0);
            bool track = false;
            if (heightMap[(int)position.x, (int)position.y] > erosionMaskHeight)
            {
                track = true;
                tmpTrackCounter--;
            }

            for (int lifetime = 0; lifetime < maxLifeTime; lifetime++)
            {
                int2 oldIntPos = new int2((int)position.x, (int)position.y);
                float oldU = position.x - oldIntPos.x;
                float oldV = position.y - oldIntPos.y;

                /* Old gradient SE */
                gradients.x = heightMap[oldIntPos.x, oldIntPos.y];
                /* Old gradient SW */
                gradients.y = heightMap[oldIntPos.x + 1, oldIntPos.y];
                /* Old gradient NE */
                gradients.z = heightMap[oldIntPos.x, oldIntPos.y + 1];
                /* Old gradient NW */
                gradients.w = heightMap[oldIntPos.x + 1, oldIntPos.y + 1];

                float oldHeight = gradients.x * (1 - oldU) * (1 - oldV) + gradients.y * oldU * (1 - oldV) + gradients.z * (1 - oldU)
                                    * oldV + gradients.w * oldU * oldV;

                direction = CalculateNewDirection(gradients, oldU, oldV, direction);
                position.x += direction.x;
                position.y += direction.y;

                if (((direction.x == 0) && (direction.y == 0)) || (position.x < brushRadius) || (position.x > mapSize - brushRadius)
                                                               || (position.y < brushRadius) || (position.y > mapSize - brushRadius))
                {
                    break;
                }

                int2 newIntPos = new int2((int)position.x, (int)position.y);

                float newU = position.x - newIntPos.x;
                float newV = position.y - newIntPos.y;

                /* New gradient SE */
                gradients.x = heightMap[newIntPos.x, newIntPos.y];
                /* New gradient SW */
                gradients.y = heightMap[newIntPos.x + 1, newIntPos.y];
                /* New gradient NE */
                gradients.z = heightMap[newIntPos.x, newIntPos.y + 1];
                /* New gradient NW */
                gradients.w = heightMap[newIntPos.x + 1, newIntPos.y + 1];

                float newHeight = gradients.x * (1 - newU) * (1 - newV) + gradients.y * newU * (1 - newV) + gradients.z * (1 - newU)
                                    * newV + gradients.w * newU * newV;

                float hdiff = newHeight - oldHeight;
                float c = Mathf.Max(-hdiff, minSlope) * speed * water * capacity;

                if ((sediment > c) || (hdiff > 0))
                {
                    float amountToDeposit = (hdiff > 0) ? Mathf.Min(hdiff, sediment) : (sediment - c) * deposition;
                    sediment -= amountToDeposit;

                    heightMap[oldIntPos.x, oldIntPos.y] += amountToDeposit * (1 - oldU) * (1 - oldV);
                    heightMap[oldIntPos.x + 1, oldIntPos.y] += amountToDeposit * oldU * (1 - oldV);
                    heightMap[oldIntPos.x, oldIntPos.y + 1] += amountToDeposit * (1 - oldU) * oldV;
                    heightMap[oldIntPos.x + 1, oldIntPos.y + 1] += amountToDeposit * oldU * oldV;
                }
                else
                {
                    float amountToErode = Mathf.Min((c - sediment) * erosionSpeed, -hdiff);

                    for (int i = 0; i < brushIndexOffsets.Count; i++)
                    {
                        int2 erodePos = newIntPos + brushIndexOffsets[i];

                        float weightedErode = amountToErode * brushWeights[i];
                        float deltaSediment = (heightMap[erodePos.x, erodePos.y] < weightedErode) ? heightMap[erodePos.x, erodePos.y] : weightedErode;
                        heightMap[erodePos.x, erodePos.y] -= deltaSediment;
                        sediment += deltaSediment;
                    }
                }
                if (track && (tmpTrackCounter >= 0))
                {
                    erosionMask.SetPixel(oldIntPos.y, oldIntPos.x, new Color(1.0f, 1.0f, 1.0f));
                }
                speed = Mathf.Sqrt(Mathf.Max(0, speed * speed + hdiff * gravity));
                water *= 1 - evaporateSpeed;
            }
        }

        erosionMask.Apply();
        byte[] bytes = erosionMask.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/Resources/Textures/ErosionMask.png", bytes);
        AssetDatabase.Refresh();
    }
}

