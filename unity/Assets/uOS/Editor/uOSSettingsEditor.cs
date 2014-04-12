using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


/// <summary>
/// Editor for uOSSettings.
/// </summary>
[CustomEditor(typeof(UOS.Settings))]
public class uOSSettingsEditor : Editor
{
    private UOS.Settings instance;

    /// <summary>
    /// Draws inspector GUI.
    /// </summary>
    public override void OnInspectorGUI()
    {
        instance = (UOS.Settings)target;

        DrawDefaultInspector();
    }
}
