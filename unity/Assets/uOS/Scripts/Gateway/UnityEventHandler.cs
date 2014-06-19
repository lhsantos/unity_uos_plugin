using System.Collections.Generic;


namespace UOS
{
    public abstract class UnityEventHandler : IUnityUpdatable
    {
        protected struct LogEvent
        {
            public string message;
            public System.Exception exception;
            public string stackTrace;

            public LogEvent(string message = "", System.Exception exception = null, string stackTrace = null)
            {
                this.message = message;
                this.exception = exception;
                this.stackTrace = stackTrace;

                if ((stackTrace == null) && (exception != null))
                    this.stackTrace = exception.StackTrace;
            }
        }


        protected Logger logger;

        protected Queue<object> eventQueue = new Queue<object>();
        protected object _events_lock = new object();


        public virtual void Update()
        {
            lock (_events_lock)
            {
                while (eventQueue.Count > 0)
                {
                    object o = eventQueue.Dequeue();
                    if (o is LogEvent)
                    {
                        var e = (LogEvent)o;
                        string msg = e.message;
                        bool error = e.exception != null;

                        if (error)
                            msg += e.exception.Message;
                        if (e.stackTrace != null)
                            msg += "\n" + e.stackTrace;

                        if (error)
                            logger.LogError(msg);
                        else
                            logger.Log(msg);
                    }
                    else
                        HandleEvent(o);
                }
            }
        }

        protected UnityEventHandler(Logger logger)
        {
            this.logger = logger;
        }

        protected void PushEvent(object evt)
        {
            lock (_events_lock)
            {
                eventQueue.Enqueue(evt);
            }
        }

        protected void PushLog(string message = "", System.Exception exception = null, string stackTrace = null)
        {
            PushEvent(new LogEvent(message, exception, stackTrace));
        }

        protected abstract void HandleEvent(object o);
    }
}
