using System.Collections.Generic;


namespace UOS
{
    /// <summary>
    /// MonoBehaviour to interface with iOS
    /// </summary>
    public class UnityGateway : Gateway
    {
        private uOSSettings settings;
        private Logger logger = new UnityLogger();
        private NetworkRadar radar = null;

        public UnityGateway(uOSSettings uOSSettings)
        {
            this.settings = uOSSettings;

            PrepareRadar();
        }

        /// <summary>
        /// Initialises this Gateway.
        /// </summary>
        public void Init()
        {
            if (radar != null)
            {
                radar.DevicesChanged += OnRadarEvent;
                radar.StartRadar();
            }
        }

        private void PrepareRadar()
        {
            switch (settings.radarType)
            {
                case RadarType.MULTICAST:
                    radar = new MulticastRadar(logger);
                    break;

                default:
                    break;
            }
        }

        private void OnRadarEvent(object sender, RadarEvent type, NetworkDevice device)
        {
            logger.Log(type.ToString() + ": " + device.networkDeviceName);
        }
    }
}
