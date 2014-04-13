using UnityEngine;

namespace UOS
{
    /// <summary>
    /// UOS logger for Unity console.
    /// </summary>
    public sealed class UnityLogger : Logger
    {
        public void Log(object message)
        {
            Debug.Log(message);
        }

        public void LogError(object message)
        {
            Debug.LogError(message);
        }

        public void LogException(System.Exception exception)
        {
            Debug.LogException(exception);
        }

        public void LogWarning(object message)
        {
            Debug.LogWarning(message);
        }
    }
}
