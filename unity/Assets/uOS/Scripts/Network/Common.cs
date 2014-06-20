namespace UOS.Net.Sockets
{
    public class SocketException : System.Exception
    {
        public SocketException(System.Exception e)
            : base(null, e) { }
    }
}
