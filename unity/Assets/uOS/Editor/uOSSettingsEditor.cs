using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


/// <summary>
/// Editor for uOSSettings.
/// </summary>
[CustomEditor(typeof(UOS.uOSSettings))]
public class uOSSettingsEditor : Editor
{
    private SerializedProperty nameProp;
    private SerializedProperty tcpPortProp;
    private SerializedProperty tcpPortRangeProp;
    private SerializedProperty udpPortProp;
    private SerializedProperty udpPortRangeProp;
    private SerializedProperty rtpPortRangeProp;
    private SerializedProperty webHostNameProp;
    private SerializedProperty webPortProp;
    private SerializedProperty radarTypeProp;
    private SerializedProperty driversProp;

    private bool toggleNetwork = true;
    private bool toggleDrivers = true;
    private string driverClass = "";
    private HashSet<string> driverClasses = new HashSet<string>();

    void OnEnable()
    {
        nameProp = serializedObject.FindProperty("deviceName");
        tcpPortProp = serializedObject.FindProperty("eth.tcp.port");
        tcpPortRangeProp = serializedObject.FindProperty("eth.tcp.passivePortRange");
        udpPortProp = serializedObject.FindProperty("eth.udp.port");
        udpPortRangeProp = serializedObject.FindProperty("eth.udp.passivePortRange");
        rtpPortRangeProp = serializedObject.FindProperty("eth.rtp.passivePortRange");
        webHostNameProp = serializedObject.FindProperty("websocket.hostName");
        webPortProp = serializedObject.FindProperty("websocket.port");
        radarTypeProp = serializedObject.FindProperty("radarType");
        driversProp = serializedObject.FindProperty("drivers");

        driverClasses.Clear();
        int size = driversProp.arraySize;
        for (int i = 0; i < size; ++i)
            driverClasses.Add(driversProp.GetArrayElementAtIndex(i).stringValue);
    }

    /// <summary>
    /// Draws inspector GUI.
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.BeginVertical();

        // The device name.
        string name = EditorGUILayout.TextField("Device name:", nameProp.stringValue, GUILayout.ExpandWidth(true));
        nameProp.stringValue = (name != null) ? name.Trim() : null;

        // Network interfaces.
        toggleNetwork = EditorGUILayout.Foldout(toggleNetwork, "Network Interfaces");
        if (toggleNetwork)
        {
            EditorGUI.indentLevel++;
            PortProp(tcpPortProp, "TCP port:");
            PortRangeProp(tcpPortRangeProp, "TCP passive range:");
            PortProp(udpPortProp, "UDP port:");
            PortRangeProp(udpPortRangeProp, "UDP passive range:");
            PortRangeProp(rtpPortRangeProp, "RTP passive range:");
            string host = EditorGUILayout.TextField("WebSocket Host:", webHostNameProp.stringValue, GUILayout.ExpandWidth(true));
            webHostNameProp.stringValue = GetValidHost(host);
            PortProp(webPortProp, "WebSocket Port:"); 
            EditorGUI.indentLevel--;
        }

        //Radar type.
        radarTypeProp.enumValueIndex =
            (int)(UOS.RadarType)EditorGUILayout.EnumPopup("Radar type:", (UOS.RadarType)radarTypeProp.enumValueIndex);

        DrawDriversUI();

        EditorGUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();
    }

    private string GetValidHost(string host)
    {
        host = (host ?? "").Trim();
        if (System.Uri.CheckHostName(host) == System.UriHostNameType.Unknown)
            host = "localhost";

        return host;
    }

    private void PortProp(SerializedProperty prop, string label)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.ExpandWidth(false), GUILayout.MaxWidth(150));
        prop.intValue = Mathf.Clamp(EditorGUILayout.IntField(prop.intValue, GUILayout.MaxWidth(60)), 1025, 65535);
        EditorGUILayout.EndHorizontal();
    }

    private void PortRangeProp(SerializedProperty prop, string label)
    {
        var centeredStyle = GUI.skin.GetStyle("Label");
        centeredStyle.alignment = TextAnchor.MiddleCenter;

        string[] strValues = prop.stringValue.Split('-');
        int[] values = new int[] { int.Parse(strValues[0]), int.Parse(strValues[1]) };

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.ExpandWidth(false), GUILayout.MaxWidth(150));
        int low = Mathf.Clamp(
            EditorGUILayout.IntField(values[0], GUILayout.MaxWidth(60), GUILayout.ExpandWidth(true)),
            1025, 65535);
        EditorGUI.indentLevel--;
        EditorGUILayout.LabelField("-", centeredStyle, GUILayout.MaxWidth(20));
        int high = Mathf.Clamp(
            EditorGUILayout.IntField(values[1], GUILayout.MaxWidth(60)),
            low, 65535);
        EditorGUI.indentLevel++;
        EditorGUILayout.EndHorizontal();

        prop.stringValue = low + "-" + high;
    }

    private void DrawDriversUI()
    {
        toggleDrivers = EditorGUILayout.Foldout(toggleDrivers, "Drivers");
        if (toggleDrivers)
        {
            // Add new driver bar.
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Class:", GUILayout.MaxWidth(40));
            driverClass = EditorGUILayout.TextField(driverClass, GUILayout.ExpandWidth(true));
            string error;
            bool valid = ValidadeDriverClass(driverClass, out error);
            GUI.enabled = valid;
            if (GUILayout.Button("Add", GUILayout.ExpandWidth(false)))
            {
                int n = driversProp.arraySize;
                driversProp.InsertArrayElementAtIndex(n);
                driversProp.GetArrayElementAtIndex(n).stringValue = driverClass;
                driverClasses.Add(driverClass);
                driverClass = "";
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // Possible errors.
            if (error != null)
            {
                var old = GUI.color;
                GUI.color = new Color32(0xFF, 0x66, 0x33, 0xFF);
                EditorGUILayout.LabelField("error: " + error + "!");
                GUI.color = old;
            }

            // Driver list.
            EditorGUI.indentLevel++;
            int size = driversProp.arraySize;
            if (size == 0)
                EditorGUILayout.LabelField("No drivers added to the list.", BoxGUIStyle(), GUILayout.ExpandWidth(true));
            else
            {
                int toRemove = -1;
                for (int i = 0; i < size; ++i)
                {
                    string driverName = driversProp.GetArrayElementAtIndex(i).stringValue;
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.SelectableLabel(
                        driverName, BoxGUIStyle(),
                            GUILayout.ExpandWidth(true), GUILayout.MaxHeight(20));
                    if (GUILayout.Button("-", GUILayout.ExpandWidth(false)))
                        toRemove = i;
                    EditorGUILayout.EndHorizontal();
                }
                if (toRemove >= 0)
                {
                    driverClasses.Remove(driversProp.GetArrayElementAtIndex(toRemove).stringValue);
                    driversProp.DeleteArrayElementAtIndex(toRemove);
                }
            }
            EditorGUI.indentLevel--;
        }
    }

    private bool ValidadeDriverClass(string className, out string error)
    {
        error = null;
        if (className == null)
            return false;
        className = className.Trim();
        if (className.Length == 0)
            return false;

        System.Type type = null;
        try { type = UOS.Util.GetType(className); }
        catch (System.Exception) { }

        if (type == null)
            error = "invalid class name or type not found";
        else if (type.GetInterface("UOS.UOSDriver") == null)
            error = "does not implement UOSDriver";
        else if ((!type.IsClass) || (type.IsAbstract))
            error = "not a concrete class";
        else if (driverClasses.Contains(driverClass))
            error = "already added this driver";

        return error == null;
    }

    private static GUIStyle BoxGUIStyle()
    {
        var labelst = new GUIStyle("Label");
        var boxst = new GUIStyle("Box");

        boxst.normal.textColor = labelst.normal.textColor;
        boxst.alignment = TextAnchor.MiddleLeft;
        boxst.border.top = boxst.border.bottom = 1;
        boxst.margin.top = boxst.margin.bottom = 1;
        boxst.padding.top = boxst.padding.bottom = 1;

        return boxst;
    }
}
