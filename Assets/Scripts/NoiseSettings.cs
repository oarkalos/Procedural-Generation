using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class NoiseSettings : ScriptableObject
{
    public bool useFirstLayerAsMask = false;
    [HideInInspector]
    public bool showSettings = false;
    public bool layerActive = true;
    [Header("Noise Settings")]
    public int octavesSize = 7;
    public float lacunarity = 2;
    public float frequency = 1;
    [Range(0, 1)]
    public float persistence = 0.5f;
    public int seed = 0;
    public bool useHeightCurve = false;
    [DisableIf("useHeightCurve", false)]
    public AnimationCurve heightCurve;
    public bool Turbulance = false;

    //[DisableIf("Turbulance", false)]
    public bool Ridges = false;

    //[DisableIf("Turbulance", false)]
    [Range(1, 5)]
    public int RidgesStrength = 1;
    public typeOfNoise noiseType;
}
