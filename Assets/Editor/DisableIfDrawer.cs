using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(DisableIf))]
public class DisableIfDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        DisableIf disableIf = attribute as DisableIf;
        SerializedProperty attributeProperty = property.serializedObject.FindProperty(disableIf.attribute);

        bool newState = true;
        bool oldState = GUI.enabled;

        if (attributeProperty == null)
        {
            Debug.LogWarning("[DisableIf] Invalid Property Name for Attribute.", attributeProperty.serializedObject.targetObject);
        }
        else
        {
            newState = attributeProperty.boolValue != disableIf.disabled;
        }

        GUI.enabled = newState;
        EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = oldState;
    }
}
