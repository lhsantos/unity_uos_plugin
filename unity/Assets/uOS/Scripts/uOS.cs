using MiniJSON;
using System.Collections.Generic;
using UnityEngine;


namespace UOS
{
    /// <summary>
    /// The main uOS API for Unity.
    /// </summary>
    public sealed class uOS
    {
        private static uOS _instance;
        static uOS instance
        {
            get
            {
                if (_instance == null)
                    throw new System.InvalidOperationException("You must call Init() before doing anything with uOS.");

                return _instance;
            }
        }


        private Gateway gateway;

        public static void Init()
        {
            if (_instance != null)
                Debug.LogWarning("uOS already initiated!");
            else
            {
                _instance = new uOS();
                _instance.gateway.Init();
            }
        }

        private uOS()
        {
            gateway = new UnityGateway(uOSSettings.instance);
        }
    }
}
