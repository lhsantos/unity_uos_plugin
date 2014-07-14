using MiniJSON;
using System.Collections.Generic;
using System.Threading;
using System.Text;


namespace UOS
{
    public class GatewayServer : UnityEventHandler
    {
        private UnityGateway gateway;
        private bool running;
        private List<ServerThreadData> threads;



        public GatewayServer(UnityGateway gateway)
            : base(gateway.logger)
        {
            this.gateway = gateway;
        }

        public void Init()
        {
            if (!running)
            {
                running = true;
                threads = new List<ServerThreadData>();
                TCPChannelManager cm = (TCPChannelManager)gateway.GetChannelManager("Ethernet:TCP");
                foreach (var device in cm.ListNetworkDevices())
                {
                    ServerThreadData std = new ServerThreadData(this, device);
                    threads.Add(std);
                }
            }
        }

        public void TearDown()
        {
            foreach (var t in threads)
                t.thread.Abort();

            threads.Clear();
            running = false;
        }

        protected override void HandleEvent(object o)
        {
            throw new System.InvalidOperationException("Unexpected event on GatewayServer!");
        }

        private void HandleMessage(string message, ClientConnection connection)
        {
            if ((message == null) || ((message = message.Trim()).Length == 0) ||
                (connection == null) || (!connection.connected))
                return;

            NetworkDevice clientDevice = connection.clientDevice;
            Message response = null;
            try
            {
                logger.Log("Handling incoming message:\n" + message);
                object json = Json.Deserialize(message);
                string type = Util.JsonOptString(json as IDictionary<string, object>, "type");
                if (type != null)
                {
                    Message.Type messageType = (Message.Type)System.Enum.Parse(typeof(Message.Type), type, true);
                    switch (messageType)
                    {
                        case Message.Type.SERVICE_CALL_REQUEST:
                            logger.Log("Incoming Service Call");
                            CallContext messageContext = new CallContext();
                            messageContext.callerNetworkDevice = clientDevice;
                            response = HandleServiceCall(message, messageContext);
                            break;

                        case Message.Type.NOTIFY:
                            logger.Log("Incoming Notify");
                            HandleNotify(message, clientDevice);
                            break;

                        default:
                            break;
                    }
                }
            }
            catch (System.Exception ex)
            {
                PushLog("Failure to handle the incoming message. ", ex);

                response = new Notify();
                response.error = "Failure to handle the incoming message. ";
            }

            if (response != null)
            {
                string msg = Json.Serialize(response.ToJSON()) + "\n";
                byte[] bytes = Encoding.UTF8.GetBytes(msg);
                try
                {
                    connection.Write(bytes, 0, bytes.Length);
                    PushLog("Responded successfully.");
                }
                catch (System.Exception e)
                {
                    PushLog("Error while responding. ", e);
                }
            }
        }

        private Response HandleServiceCall(string message, CallContext messageContext)
        {
            try
            {
                Call serviceCall = Call.FromJSON(Json.Deserialize(message));
                Response response = gateway.HandleServiceCall(serviceCall, messageContext);
                logger.Log("Returning service response");
                return response;
            }
            catch (System.Exception e)
            {
                PushLog("Internal Failure: ", e);

                Response errorResponse = new Response();
                errorResponse.error = e.Message == null ? "Internal Error" : e.Message;
                return errorResponse;
            }
        }

        private void HandleNotify(string message, NetworkDevice clientDevice)
        {
            try
            {
                Notify notify = Notify.FromJSON(Json.Deserialize(message));
                UpDevice device = gateway.RetrieveDevice(
                    Util.GetHost(clientDevice.networkDeviceName),
                    clientDevice.networkDeviceType);

                gateway.HandleNotify(notify, device);
            }
            catch (System.Exception e)
            {
                PushLog("Internal Failure. Notify cannot be handled. ", e);
            }
        }


        private class ServerThreadData
        {
            public GatewayServer gatewayServer { get; private set; }
            public NetworkDevice device { get; private set; }
            public Thread thread { get; private set; }


            public ServerThreadData(GatewayServer gatewayServer, NetworkDevice device)
            {
                this.gatewayServer = gatewayServer;
                this.device = device;

                thread = new Thread(new ThreadStart(ConnectionThread));
                thread.Start();
            }

            private void ConnectionThread()
            {
                ClientConnection con = null;
                while (gatewayServer.running)
                {
                    try
                    {
                        // This blocks the thread until some client connects...
                        con = gatewayServer.gateway.OpenPassiveConnection(device.networkDeviceName, device.networkDeviceType);
                        if (con != null)
                        {
                            gatewayServer.PushLog(
                                    "Connection received from an ubiquitos-client device: '" +
                                    con.clientDevice.networkDeviceName + "' on '" + con.clientDevice.networkDeviceType + "'.");
                        }
                        else
                            throw new System.Exception("Couldn't establish connection to client.");

                        if (con.connected)
                        {
                            StringBuilder builder = new StringBuilder();
                            byte[] data = con.Read();
                            if ((data != null) && (data.Length > 0))
                            {
                                int read = data.Length;
                                string[] msgs = Encoding.UTF8.GetString(data, 0, read).Split('\n');
                                int last = msgs.Length - 1;
                                int i = 0;
                                byte lastByte = data[read - 1];

                                // Were we waiting for the rest of a message?
                                if (builder.Length > 0)
                                {
                                    builder.Append(msgs[i++]);
                                    if ((msgs.Length > 1) || (lastByte == '\n'))
                                    {
                                        gatewayServer.HandleMessage(builder.ToString(), con);
                                        builder.Length = 0;
                                    }
                                }

                                // Processes intermediate messages...
                                for (; i < last; ++i)
                                    gatewayServer.HandleMessage(msgs[i], con);

                                // Processes the last chunk...
                                if (i == last)
                                {
                                    builder.Append(msgs[i]);
                                    if (lastByte == '\n')
                                    {
                                        gatewayServer.HandleMessage(builder.ToString(), con);
                                        builder.Length = 0;
                                    }
                                }
                            }
                            con.Close();
                        }
                    }
                    catch (System.Threading.ThreadAbortException)
                    {
                        if ((con != null) && (con.connected))
                            con.Close();
                        return;
                    }
                    catch (System.Exception e)
                    {
                        if ((con != null) && (con.connected))
                            con.Close();
                        gatewayServer.PushLog("Failed to handle ubiquitos-smartspace connection. ", e);
                    }
                }
            }
        }
    }
}
