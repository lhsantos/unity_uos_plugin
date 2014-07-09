using System.Collections.Generic;
using UnityEngine;
using UOS;


[RequireComponent(typeof(uOS))]
public class TestUOS : MonoBehaviour, Logger, UOSApplication
{
    UnityGateway gateway;
    string myLog = "";

    /// <summary>
    /// Called right before the first update.
    /// </summary>
    void Start()
    {
        uOS.Init(this, this);
    }

    /// <summary>
    /// Called once every frame.
    /// </summary>
    void Update()
    {
    }

    void OnGUI()
    {
        int w = Screen.width, h = Screen.height;
        Vector2 border = new Vector2(10, 10);
        Vector2 halfArea = new Vector2(w / 2.0f - border.x, h / 2.0f - border.y);
        Rect devRect = new Rect(border.x, border.y, halfArea.x * 2, halfArea.y);
        Rect logRect = new Rect(border.x, border.y + halfArea.y, halfArea.x * 2, halfArea.y);


        List<UpDevice> devices = new List<UpDevice>(uOS.gateway.ListDevices());

        var builder = new System.Text.StringBuilder();
        builder.AppendLine("Known devices:\n");
        foreach (var d in devices)
        {
            builder.AppendLine(MiniJSON.Json.Serialize(d.ToJSON()));
            builder.AppendLine();
        }

        GUI.TextArea(devRect, builder.ToString());
        GUI.TextArea(logRect, myLog);
    }



    public void Log(object message)
    {
        DoLog(message.ToString());
    }

    public void LogError(object message)
    {
        DoLog("ERROR: " + message);
    }

    public void LogException(System.Exception exception)
    {
        DoLog("ERROR: " + exception.StackTrace);
    }

    public void LogWarning(object message)
    {
        DoLog("WARNING: " + message);
    }

    private void DoLog(string msg)
    {
        Debug.Log(msg);
        myLog = msg + "\n" + myLog;
    }


    void UOSApplication.Init(IGateway gateway, uOSSettings settings)
    {
        this.gateway = gateway as UnityGateway;
    }

    void UOSApplication.TearDown()
    {
    }


    /// <summary>
    /// App service example!
    /// </summary>
    /// <param name="serviceCall"></param>
    /// <param name="serviceResponse"></param>
    /// <param name="messageContext"></param>
    public void AppCall(Call serviceCall, Response serviceResponse, CallContext messageContext)
    {
        Log("AppCall!");
    }
}
