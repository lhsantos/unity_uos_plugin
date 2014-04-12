using System.Collections.Generic;
using UnityEngine;


namespace UOS
{
    /// <summary>
    /// MonoBehaviour to interface with iOS
    /// </summary>
    public class UnityGateway : MonoBehaviour, Gateway
    {
        /// <summary>
        /// Which type of Radar should this UnityGateway use?
        /// </summary>
        public enum RadarType
        {
            MULTICAST,
            ARP,
            PING,
            NONE
        }

        /// <summary>
        /// The type of Radar this gateway should use.
        /// </summary>
        public RadarType radarType = RadarType.MULTICAST;


        private UnityRadar radar = null;


        /// <summary>
        /// Called when this object is created.
        /// </summary>
        void Awake()
        {
            PrepareRadar();
        }

        /// <summary>
        /// Called right before the first update.
        /// </summary>
        void Start()
        {
            if (radar)
            {
                radar.DevicesChanged += OnRadarEvent;
                radar.StartRadar();
            }
        }

        /// <summary>
        /// Called once every frame.
        /// </summary>
        void Update()
        {
        }


        private void PrepareRadar()
        {
            switch (radarType)
            {
                case RadarType.MULTICAST:
                    radar = gameObject.AddComponent<MulticastRadar>();
                    break;

                default:
                    break;
            }
        }

        private void OnRadarEvent(object sender, RadarEvent type, NetworkDevice device)
        {
            Debug.Log(type.ToString() + ": " + device.networkDeviceName);
        }
    }
}
