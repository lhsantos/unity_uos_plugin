//#if UNITY_ANDROID && !UNITY_EDITOR
#if FALSE

using System.Collections.Generic;
using System.Threading;
using UnityEngine;


namespace UOS.Net
{
    public class IPAddress
    {
        private static IPAddress _any = null;
        public static IPAddress Any
        {
            get
            {
                if (_any == null)
                    _any = new IPAddress(new byte[] { 0, 0, 0, 0 });
                return _any;
            }
        }

        private static IPAddress _broadcast = null;
        public static IPAddress Broadcast
        {
            get
            {
                if (_broadcast == null)
                    _broadcast = new IPAddress(new byte[] { 255, 255, 255, 255 });
                return _broadcast;
            }
        }

        private static AndroidJavaClass _inetAddrClass = null;
        private static AndroidJavaClass inetAddrClass
        {
            get
            {
                if (_inetAddrClass == null)
                    _inetAddrClass = new AndroidJavaClass("java.net.InetAddress");
                return _inetAddrClass;
            }
        }

        private AndroidJavaObject internalAddress;


        private IPAddress(AndroidJavaObject internalAddress)
        {
            this.internalAddress = internalAddress;
        }

        private IPAddress(byte[] addr)
            : this(inetAddrClass.CallStatic<AndroidJavaObject>("getByAddress", addr)) { }

        public override string ToString()
        {
            return internalAddress.Call<string>("getHostAddress");
        }

        public static IPAddress Parse(string ipString)
        {
            return new IPAddress(inetAddrClass.CallStatic<AndroidJavaObject>("getByName", ipString));
        }

        public static string GetLocalHost()
        {
            try { return GetLocal().internalAddress.Call<string>("getHostName"); }
            catch (System.Exception) { return "localhost"; }
        }

        /// <summary>
        /// May thrown an Android exception if Internet permission is not set.
        /// </summary>
        /// <returns></returns>
        public static IPAddress GetLocal()
        {
            return new IPAddress(inetAddrClass.CallStatic<AndroidJavaObject>("getLocalHost"));
        }
    }

    public class IPEndPoint
    {
        public IPEndPoint(IPAddress address, int port)
        {
            this.Address = address;
            this.Port = port;
        }

        public IPAddress Address { get; private set; }

        public int Port { get; private set; }

        public override string ToString()
        {
            return Address.ToString() + ":" + Port;
        }
    }
}

namespace UOS.Net.Sockets
{
    public class NetworkStream
    {
        private AndroidJavaObject inputStream;
        private AndroidJavaObject outputStream;

        public NetworkStream(AndroidJavaObject inputStream, AndroidJavaObject outputStream)
        {
            this.inputStream = inputStream;
            this.outputStream = outputStream;
        }

        public bool CanRead { get { return inputStream != null; } }

        public bool CanWrite { get { return outputStream != null; } }

        public bool DataAvailable { get { return inputStream.Call<int>("available") > 0; } }

        public int Read(byte[] buffer, int offset, int size)
        {
            return inputStream.Call<int>("read", buffer, offset, size);
        }

        public void Write(byte[] buffer, int offset, int size)
        {
            outputStream.Call("write", buffer, offset, size);
        }
    }

    public class TcpClient
    {
        AndroidJavaObject internalClient;

        public TcpClient(AndroidJavaObject internalClient)
        {
            this.internalClient = internalClient;
        }

        public TcpClient()
            : this(new AndroidJavaObject("org.unbiquitous.unity.androidnetwork.TcpClient")) { }

        public string Host
        {
            get { return internalClient.Call<string>("getHost"); }
        }

        public int Port
        {
            get { return internalClient.Call<int>("getPort"); }
        }

        public bool Connected { get { return internalClient.Call<bool>("isConnected"); } }

        public int ReceiveTimeout
        {
            get { return internalClient.Call<int>("getReceiveTimout"); }
            set { internalClient.Call("setReceiveTimout", value); }
        }

        public int SendTimeout
        {
            get { return internalClient.Call<int>("getSendTimout"); }
            set { internalClient.Call("setSendTimout", value); }
        }

        public bool ReuseAddress
        {
            get { return internalClient.Call<bool>("getReuseAddress"); }
            set { internalClient.Call("setReuseAddress", value); }
        }


        public void Connect(string host, int port)
        {
            internalClient.Call("connect", host, port);
        }

        public NetworkStream GetStream()
        {
            var inputStream = internalClient.Call<AndroidJavaObject>("createInputStream");
            var outputStream = internalClient.Call<AndroidJavaObject>("createOutputStream");

            return new NetworkStream(inputStream, outputStream);
        }

        public void Close()
        {
            internalClient.Call("close");
        }
    }

    public class TcpListener
    {
        private AndroidJavaObject internalListener;

        public TcpListener(string host, int port)
        {
            internalListener = new AndroidJavaObject("org.unbiquitous.unity.androidnetwork.TcpListener", host, port);
        }

        public bool ReuseAddress
        {
            get { return internalListener.Call<bool>("getReuseAddress"); }
            set { internalListener.Call("setReuseAddress", value); }
        }

        public TcpClient AcceptTcpClient()
        {
            return new TcpClient(internalListener.Call<AndroidJavaObject>("acceptTcpClient"));
        }

        public bool Pending()
        {
            return internalListener.Call<bool>("isPending");
        }

        public void Start()
        {
            internalListener.Call("start");
        }

        public void Stop()
        {
            internalListener.Call("stop");
        }
    }

    public class UdpClient
    {
        private class UdpClientAsyncResult : System.IAsyncResult
        {
            public object AsyncState { get; private set; }

            public WaitHandle AsyncWaitHandle { get { throw new System.NotImplementedException(); } }

            public bool CompletedSynchronously { get { throw new System.NotImplementedException(); } }

            public bool IsCompleted { get { throw new System.NotImplementedException(); } }

            public UdpClientAsyncResult(object state)
            {
                AsyncState = state;
            }
        }

        private Dictionary<System.IAsyncResult, int> pendingOperations = new Dictionary<System.IAsyncResult, int>();
        private AndroidJavaObject internalClient;

        public UdpClient(IPEndPoint localEP)
        {
            internalClient = new AndroidJavaObject(
                "org.unbiquitous.unity.androidnetwork.UdpClient", localEP.Address.ToString(), localEP.Port);
        }

        public bool EnableBroadcast
        {
            get { return internalClient.Call<bool>("getEnableBroadcast"); }
            set { internalClient.Call("setEnableBroadcast", value); }
        }

        public bool Connected { get { return internalClient.Call<bool>("isConnected"); } }

        public bool ReuseAddress
        {
            get { return internalClient.Call<bool>("getReuseAddress"); }
            set { internalClient.Call("setReuseAddress", value); }
        }

        public int ReceiveTimeout
        {
            get { return internalClient.Call<int>("getReceiveTimout"); }
            set { internalClient.Call("setReceiveTimout", value); }
        }

        public void Connect(IPEndPoint endPoint)
        {
            internalClient.Call("connect", endPoint.Address.ToString(), endPoint.Port);
        }

        public System.IAsyncResult BeginSend(byte[] datagram, int bytes, IPEndPoint ep, System.AsyncCallback requestCallback, object state)
        {
            System.IAsyncResult result = new UdpClientAsyncResult(state);
            new Thread(
                new ThreadStart(delegate()
                {
#if UNITY_ANDROID
                    AndroidJNI.AttachCurrentThread();
                    try
                    {
#endif
                        int written = internalClient.Call<int>("send", datagram, bytes, ep.Address.ToString(), ep.Port);
                        pendingOperations[result] = written;
                        requestCallback(result);
#if UNITY_ANDROID
                    }
                    catch (System.Exception)
                    {
                        UnityEngine.AndroidJNI.DetachCurrentThread();
                        throw;
                    }
                    UnityEngine.AndroidJNI.DetachCurrentThread();
#endif
                })
            ).Start();

            return result;
        }

        public int EndSend(System.IAsyncResult asyncResult)
        {
            int written = 0;
            if ((asyncResult != null) && pendingOperations.TryGetValue(asyncResult, out written))
                pendingOperations.Remove(asyncResult);

            return written;
        }

        public byte[] Receive(ref IPEndPoint remoteEP)
        {
            try
            {
                AndroidJavaObject received = internalClient.Call<AndroidJavaObject>("receive");
                IPAddress address = IPAddress.Parse(received.Call<string>("getAddress"));
                int port = received.Call<int>("getPort");
                remoteEP = new IPEndPoint(address, port);
                return received.Call<byte[]>("getData");
            }
            catch (AndroidJavaException e)
            {
                if (e.Message.Contains("SocketTimeoutException"))
                    throw new TimoutException(e);
                throw;
            }
        }

        public void Close()
        {
            internalClient.Call("close");
        }
    }
}
#endif
