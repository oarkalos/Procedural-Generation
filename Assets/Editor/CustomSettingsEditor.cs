using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GenerateTerrain))]
public class CustomSettingsEditor : Editor
{
    GenerateTerrain generateTerrain;
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        for (int i = 0; i < generateTerrain.noiseLayers.Capacity; i++)
        {
            generateTerrain.noiseLayers[i].showSettings = EditorGUILayout.InspectorTitlebar(generateTerrain.noiseLayers[i].showSettings,
                generateTerrain.noiseLayers[i]);
            if (generateTerrain.noiseLayers[i].showSettings)
            {
                Editor editor = CreateEditor(generateTerrain.noiseLayers[i]);
                editor.OnInspectorGUI();
            }
        }

        if (GUILayout.Button("Generate Terrain"))
        {
            generateTerrain.CreateTerrain();
        }
        if (GUILayout.Button("Generate HeightMap"))
        {
            generateTerrain.CreateHeightMap();
        }
    }

    private void OnEnable()
    {
        generateTerrain = (GenerateTerrain)target;
    }
}
