using System.Collections.Generic;
using UnityEngine;
using UOS;


/// <summary>
/// The main uOS API for Unity.
/// </summary>
public sealed class uOS : MonoBehaviour
{
    private static Logger _logger;
    private static uOS _instance;

    private static Logger logger
    {
        get
        {
            if (_logger == null)
                _logger = new UnityLogger();
            return _logger;
        }

        set { _logger = value; }
    }

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

    /// <summary>
    /// The current settings for uOS.
    /// </summary>
    public static uOSSettings settings { get { return uOSSettings.instance; } }

    /// <summary>
    /// Is there a valid uOS instance and is it ready?
    /// </summary>
    public static bool ready { get { return instance._ready; } }

    /// <summary>
    /// If there is a valid ready uOS instance, retrieves its gateway.
    /// </summary>
    public static IGateway gateway { get { return readyInstance._gateway; } }

    private bool _ready;
    private UnityGateway _gateway;

    /// <summary>
    /// Called when this game component is created at the scene.
    /// </summary>
    void Awake()
    {
        if (_instance == null)
            _instance = this;
        else
            throw new System.InvalidOperationException("The scene must not contain more than one instance of uOS");
    }

    /// <summary>
    /// Called when this game component is disabled at the scene.
    /// </summary>
    void OnDisable()
    {
        CheckDestroy();
    }

    /// <summary>
    /// Called when this game component is destroyed.
    /// </summary>
    void OnDestroy()
    {
        CheckDestroy();
    }

    /// <summary>
    /// Tears down the middleware when the game component is no longer valid.
    /// </summary>
    private void CheckDestroy()
    {
        if (_instance == this)
        {
            TearDown();
            _instance = null;
        }
    }

    /// <summary>
    /// Initialises the uOS middleware with given app call handler and optional logger.
    /// </summary>
    /// <param name="app">The instance of of UOSApplication that will handle app service calls (it may be null).</param>
    /// <param name="plogger"></param>
    public static void Init(UOSApplication app, Logger plogger = null)
    {
        if (!ready)
        {
            if (plogger != null)
                logger = plogger;

            logger.Log("uOS init");

            _instance._gateway = new UnityGateway(settings, logger, app);
            _instance._gateway.Init();

            _instance._ready = true;
        }
        else
            logger.LogWarning("uOS already initiated!");
    }

    /// <summary>
    /// Releases resources and tears down the middleware.
    /// </summary>
    public static void TearDown()
    {
        if (ready)
        {
            logger.Log("uOS tear down");

            _instance._gateway.TearDown();
            _instance._gateway = null;

            _instance._ready = false;
        }
        else
            logger.LogWarning("uOS already dead!");
    }

    /// <summary>
    /// Called on every frame of the game.
    /// </summary>
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
