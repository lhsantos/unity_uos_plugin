using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;


/// <summary>
/// Editor for uOSSettings.
/// </summary>
[CustomEditor(typeof(Uos.Settings))]
public class uOSSettingsEditor : Editor
{
    private Uos.Settings instance;

    /// <summary>
    /// Draws inspector GUI.
    /// </summary>
    public override void OnInspectorGUI()
    {
        instance = (Uos.Settings)target;

        DrawDefaultInspector();
    }
}
