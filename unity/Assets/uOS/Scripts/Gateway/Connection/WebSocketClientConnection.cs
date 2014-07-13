using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using WebSocketSharp;


namespace UOS
{
    public class WebSocketClientConnection : ClientConnection
    {
        private WebSocket socket;
        private int timeout;
        private Queue<MessageEventArgs> msgQueue = new Queue<MessageEventArgs>();
        private object _msg_lock = new object();
        private LogData lastLog = null;
        private string uuid = System.Guid.NewGuid().ToString();



        public WebSocketClientConnection(string host, int port, int timeoutMillis)
            : base(new WebSocketDevice(port, host))
        {
            this.timeout = timeoutMillis;

            socket = new WebSocket("ws://" + host + ":" + port + "/");
            socket.Log.Output = OnLog;
            socket.OnMessage += OnMessageReceived;

            socket.Connect();
            CheckError();

            // Say hi!
            socket.Send("HI:" + uuid);
            // Wait for hello...
            WaitMessage();
        }

        public override bool connected
        {
            get { return socket.IsAlive; }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public override byte[] Read()
        {
            MessageEventArgs msg = WaitMessage();
            if (msg == null)
                throw new System.TimeoutException();

            if (msg.Type == Opcode.Binary)
                return msg.RawData;
            else
            {
                string text = msg.Data;
                int pos = text.IndexOf(':');
                pos = text.IndexOf(':', pos + 1);
                return Encoding.UTF8.GetBytes(text.Substring(pos + 1));
            }
        }

        public override void Write(byte[] buffer, int offset, int size)
        {
            string msg = "MSG:" + uuid + ":" + Encoding.UTF8.GetString(buffer, offset, size);
            socket.Send(msg);
            CheckError();
        }

        public override void Close()
        {
            socket.Close();
        }

        private MessageEventArgs WaitMessage()
        {
            int time = timeout;
            MessageEventArgs msg = CheckMessage();
            while ((msg == null) && (time > 0))
            {
                Thread.Sleep(100);
                time -= 100;
                msg = CheckMessage();
            }

            return msg;
        }

        private MessageEventArgs CheckMessage()
        {
            lock (_msg_lock)
            {
                return (msgQueue.Count > 0) ? msgQueue.Dequeue() : null;
            }
        }

        private void OnMessageReceived(object sender, MessageEventArgs e)
        {
            UnityEngine.Debug.Log(e.ToString());
            lock (_msg_lock)
            {
                msgQueue.Enqueue(e);
            }
        }

        private void OnLog(LogData logData, string logFile)
        {
            lastLog = logData;
        }

        private void CheckError()
        {
            System.Exception e = null;
            if ((lastLog != null) && (lastLog.Level >= LogLevel.Error))
                e = new System.Exception(lastLog.Message);
            lastLog = null;

            if (e != null)
                throw e;
        }
    }
}
