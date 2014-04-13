using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


/// <summary>
/// Editor for uOSSettings.
/// </summary>
[CustomEditor(typeof(UOS.uOSSettings))]
public class uOSSettingsEditor : Editor
{
    private UOS.uOSSettings instance;

    /// <summary>
    /// Draws inspector GUI.
    /// </summary>
    public override void OnInspectorGUI()
    {
        instance = (UOS.uOSSettings)target;

        DrawDefaultInspector();
    }
}
