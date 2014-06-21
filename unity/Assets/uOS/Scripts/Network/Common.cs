namespace UOS.Net.Sockets
{
    public class SocketTimoutException : System.Exception
    {
        public SocketTimoutException(System.Exception e)
            : base(null, e) { }
    }
}
