namespace UOS
{
    /// <summary>
    /// UOS logger.
    /// </summary>
    public interface Logger
    {
        void Log(object message);
        void LogError(object message);
        void LogException(System.Exception exception);
        void LogWarning(object message);
    }
}
