using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;


namespace UOS
{
    /// <summary>
    /// Global settings for uOS.
    /// </summary>
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public class uOSSettings : ScriptableObject
    {
        const string uOSSettingsAssetName = "uOSSettings";
        const string uOSSettingsPath = "uOS/Resources";
        const string uOSSettingsAssetExtension = ".asset";

        private static uOSSettings _instance;
        public static uOSSettings instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load(uOSSettingsAssetName) as uOSSettings;
                    if (_instance == null)
                    {
                        // If not found, autocreate the asset object.
                        _instance = CreateInstance<uOSSettings>();
#if UNITY_EDITOR
                        string properPath = Path.Combine(Application.dataPath, uOSSettingsPath);
                        if (!Directory.Exists(properPath))
                        {
                            AssetDatabase.CreateFolder("Assets/uOS", "Resources");
                        }

                        string fullPath = Path.Combine(
                            Path.Combine("Assets", uOSSettingsPath), uOSSettingsAssetName + uOSSettingsAssetExtension);
                        AssetDatabase.CreateAsset(_instance, fullPath);
#endif
                    }
                }
                return _instance;
            }
        }

#if UNITY_EDITOR
        [MenuItem("uOS/Edit Settings")]
        public static void Edit()
        {
            Selection.activeObject = instance;
        }
#endif


        //ubiquitos.uos.deviceName=unity_uos
        public string deviceName = "unity_uos";

        //ubiquitos.radar=br.unb.unbiquitous.ubiquitos.network.ethernet.radar.EthernetArpRadar
        //ubiquitos.connectionManager=org.unbiquitous.uos.network.socket.connectionManager.EthernetTCPConnectionManager, org.unbiquitous.uos.network.socket.connectionManager.EthernetUDPConnectionManager
        ////ubiquitos.connectionManager=br.unb.unbiquitous.ubiquitos.network.bluetooth.connectionManager.BluetoothConnectionManager

        //ubiquitos.eth
        public uOSEthernetSettings eth = new uOSEthernetSettings();

        //ubiquitos.bth
        public uOSBluetoothSettings bth = new uOSBluetoothSettings();

        //ubiquitos.web
        public uOSWebSocektSettings websocket = new uOSWebSocektSettings();

        //ubiquitos.driver.deploylist=org.unbiquitous.uos.core.driver.DeviceDriverImpl;\
        //    org.unbiquitous.uos.core.driver.OntologyDriverImpl;\
        //    org.unbiquitous.uos.core.driver.UserDriver(My_user_driver);

        public RadarType radarType;

        /// <summary>
        /// Name of the driver classes to load.
        /// </summary>
        public string[] drivers;
    }

    //ubiquitos.eth.tcp
    [System.Serializable]
    public class uOSTCPSettings
    {
        //ubiquitos.eth.tcp.port=14984
        public int port = 14984;

        //ubiquitos.eth.tcp.passivePortRange=14985-15000
        public string passivePortRange = "14985-15000";
    }


    //ubiquitos.eth.udp
    [System.Serializable]
    public class uOSUDPSettings
    {
        //ubiquitos.eth.udp.port=15001
        public int port = 15001;

        //ubiquitos.eth.udp.passivePortRange=15002-15017
        public string passivePortRange = "15002-15017";
    }

    //ubiquitos.eth.rtp
    [System.Serializable]
    public class uOSRTPSettings
    {
        //ubiquitos.eth.rtp.passivePortRange=15018-15028
        public string passivePortRange = "15018-15028";
    }

    //ubiquitos.eth
    [System.Serializable]
    public class uOSEthernetSettings
    {
        //ubiquitos.eth.tcp
        public uOSTCPSettings tcp = new uOSTCPSettings();

        //ubiquitos.eth.udp
        public uOSUDPSettings udp = new uOSUDPSettings();

        //ubiquitos.eth.rtp
        public uOSRTPSettings rtp = new uOSRTPSettings();
    }

    //ubiquitos.bth
    [System.Serializable]
    public class uOSBluetoothSettings
    {
        //ubiquitos.bth.provider=AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
        public string provider = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

        //ubiquitos.bth.client=AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAB
        public string client = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAB";
    }

    //ubiquitos.web.port
    [System.Serializable]
    public class uOSWebSocektSettings
    {
        public string hostName = "localhost";

        //ubiquitos.websocket.port=8080
        public int port = 8080;

        //ubiquitos.websocket.timeout=1000
        public int timeout = 1000;
    }

    /// <summary>
    /// Which type of Radar should the gateway use?
    /// </summary>
    public enum RadarType
    {
        MULTICAST,
        ARP,
        PING,
        NONE
    }
}
