using UnityEngine;

namespace UOS
{
    public sealed class DummyLogger : Logger
    {
        public void Log(object message)
        {
        }

        public void LogError(object message)
        {
        }

        public void LogException(System.Exception exception)
        {
        }

        public void LogWarning(object message)
        {
        }
    }
}
