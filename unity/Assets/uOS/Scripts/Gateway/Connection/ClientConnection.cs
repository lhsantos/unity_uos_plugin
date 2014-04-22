using System.IO;


namespace UOS
{
    public abstract class ClientConnection
    {
        public delegate void ReadCallback(byte[] readData, object callerState, System.Exception e);

        public delegate void WriteCallback(int bytesWritten, object callerState, System.Exception e);

        public NetworkDevice clientDevice { get; protected set; }

        public abstract bool connected { get; }

        public abstract int Read(byte[] buffer, int offset, int size);

        public abstract void Write(byte[] buffer, int offset, int size);

        public abstract void ReadAsync(ReadCallback callback, object callerState);

        public abstract void WriteAsync(byte[] buffer, WriteCallback callback, object callerState);

        public abstract void Close();

        public ClientConnection(NetworkDevice clientDevice)
        {
            this.clientDevice = clientDevice;
        }
    }
}
