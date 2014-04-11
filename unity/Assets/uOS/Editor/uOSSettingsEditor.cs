using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;


/// <summary>
/// Editor for uOSSettings.
/// </summary>
[CustomEditor(typeof(uOSSettings))]
public class uOSSettingsEditor : Editor
{
    private uOSSettings instance;

    /// <summary>
    /// Draws inspector GUI.
    /// </summary>
    public override void OnInspectorGUI()
    {
        instance = (uOSSettings)target;

        DrawDefaultInspector();
    }
}
