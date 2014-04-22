using System.Collections.Generic;
using UnityEngine;


namespace UOS
{
    /// <summary>
    /// The main uOS API for Unity.
    /// </summary>
    public sealed class uOS : MonoBehaviour
    {
        private static uOS _instance;
        private static uOS instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<uOS>();

                if (_instance != null)
                    return _instance;

                throw new System.InvalidOperationException("There must be one instance of uOS in the scene.");
            }
        }

        private static uOS readyInstance
        {
            get
            {
                if (!instance._ready)
                    throw new System.InvalidOperationException("You must call uOS.Init() before any other operation!");

                return _instance;
            }
        }

        public static uOSSettings settings { get { return uOSSettings.instance; } }

        public static bool ready { get { return instance._ready; } }

        public static IGateway gateway { get { return readyInstance._gateway; } }

        private bool _ready;
        private UnityGateway _gateway;

        void Awake()
        {
            if (_instance == null)
                _instance = this;
            else
                throw new System.InvalidOperationException("The scene must not contain more than one instance of uOS");
        }

        void OnDisable()
        {
            CheckDestroy();
        }

        void OnDestroy()
        {
            CheckDestroy();
        }

        private void CheckDestroy()
        {
            if (_instance == this)
            {
                TearDown();
                _instance = null;
            }
        }

        public static void Init()
        {
            if (!ready)
            {
                Debug.Log("uOS init");

                _instance._gateway = new UnityGateway(settings);
                _instance._gateway.Init();

                _instance._ready = true;
            }
            else
                Debug.LogWarning("uOS already initiated!");
        }

        public static void TearDown()
        {
            if (ready)
            {
                Debug.Log("uOS tear down");

                _instance._gateway.TearDown();
                _instance._gateway = null;

                _instance._ready = false;
            }
            else
                Debug.LogWarning("uOS already dead!");
        }

        void Update()
        {
            if (instance != this)
                throw new System.InvalidOperationException("This is not the valid instance of uOS, you can't use more than one!");

            if (_ready)
            {
                _gateway.Update();
            }
        }
    }
}
