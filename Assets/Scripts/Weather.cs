using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.VFX;

public class Weather : MonoBehaviour
{
    public Vector2 WindDirection;
    public float WindSpeed;
    public float rainDroplets;
    public float dropletSize;
    public float snowFlakes;
    public float snowFlakeSize;
    public VisualEffect vf;
    VolumetricClouds clouds;
    Material terrainMat;

    void Start()
    {
        if (!vf)
        {
            vf = gameObject.AddComponent<VisualEffect>();
        }
        if (!clouds)
        {
            Volume volume = GameObject.Find("Clouds").GetComponent<Volume>();
            clouds = volume.profile.components[2] as VolumetricClouds;
        }
        if (!terrainMat)
        {
            terrainMat = GameObject.Find("Terrain").GetComponent<MeshRenderer>().sharedMaterial;
        }
        rainDroplets = 100f;
        dropletSize = 0.2f;
    }

    void Update()
    {
        if (clouds.cloudPreset == VolumetricClouds.CloudPresets.Stormy)
        {
            if ((!vf.visualEffectAsset) || (vf.visualEffectAsset.name != "Rain"))
            {
                vf.visualEffectAsset = (VisualEffectAsset)Resources.Load("VFX/Rain");
            }
            vf.SetFloat("Droplets", rainDroplets);
            vf.SetFloat("DropletSize", dropletSize);
            vf.SetVector2("WindDirection", WindDirection);
            vf.SetFloat("WindSpeed", WindSpeed);
            vf.Play();
        }
        else if (clouds.cloudPreset == VolumetricClouds.CloudPresets.Overcast)
        {
            if ((!vf.visualEffectAsset) || (vf.visualEffectAsset.name != "Snow"))
            {
                vf.visualEffectAsset = (VisualEffectAsset)Resources.Load("VFX/Snow");
            }
            vf.SetFloat("SnowFlakes", snowFlakes);
            vf.SetFloat("SnowFlakeSize", snowFlakeSize);
            vf.SetVector2("WindDirection", WindDirection);
            vf.SetFloat("WindSpeed", WindSpeed);
            vf.Play();
        }
        else
        {
            vf.Stop();
            vf.visualEffectAsset = null;
        }
    }

}
