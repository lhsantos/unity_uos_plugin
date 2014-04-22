using MiniJSON;
using System.Collections.Generic;
using System.Threading;
using System.Text;


namespace UOS
{
    public class GatewayServer : UnityEventHandler
    {
        private struct MessageEvent
        {
            public NetworkDevice device;
            public ClientConnection connection;
            public string message;

            public MessageEvent(NetworkDevice device, ClientConnection connection, string message)
            {
                this.device = device;
                this.connection = connection;
                this.message = message;
            }
        }

        private UnityGateway gateway;
        private bool running;
        private IList<ServerThreadData> threads;



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
                foreach (var device in gateway.GetChannelManager("Ethernet:TCP").ListNetworkDevices())
                {
                    ServerThreadData std = new ServerThreadData(this, device);
                    threads.Add(std);
                }
            }
        }

        public void TearDown()
        {
            threads.Clear();
            running = false;
        }

        protected override void HandleEvent(object o)
        {
            HandleMessage((MessageEvent)o);
        }

        private void HandleMessage(MessageEvent msgEvt)
        {
            if ((msgEvt.message == null) ||
                (msgEvt.device == null) ||
                (msgEvt.connection == null) ||
                (!msgEvt.connection.connected))
                return;

            string message = msgEvt.message;
            NetworkDevice clientDevice = msgEvt.device;
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

                        //case Message.Type.ENCAPSULATED_MESSAGE:
                        //    logger.Log("Incoming Encapsulated Message");
                        //    HandleEncapsulatedMessage(message, clientDevice);
                        //    break;

                        default:
                            break;
                    }
                }
            }
            catch (System.Exception ex)
            {
                PushEvent(new LogEvent("Failure to handle the incoming message. ", ex));

                response = new Notify();
                response.error = "Failure to handle the incoming message. ";
            }

            if (response != null)
            {
                string msg = Json.Serialize(response.ToJSON()) + "\n";
                UnityEngine.Debug.Log(msg);
                byte[] bytes = Encoding.UTF8.GetBytes(msg);
                msgEvt.connection.WriteAsync(
                    bytes,
                    new ClientConnection.WriteCallback(
                        delegate(int written, object state, System.Exception e)
                        {
                            if (e != null)
                                PushEvent(new LogEvent("Error while responding. ", e));
                            else
                                PushEvent(new LogEvent("Responded successfully."));
                        }),
                    null);
            }
        }

        private Response HandleServiceCall(string message, CallContext messageContext)
        {
            try
            {
                Call serviceCall = Call.FromJSON(Json.Deserialize(message));
                Response response = gateway.driverManager.HandleServiceCall(serviceCall, messageContext);
                logger.Log("Returning service response");
                return response;
            }
            catch (System.Exception e)
            {
                PushEvent(new LogEvent("Internal Failure: ", e));

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
                    UnityGateway.GetHost(clientDevice.networkDeviceName),
                    clientDevice.networkDeviceType);

                gateway.HandleNotify(notify, device);
            }
            catch (System.Exception e)
            {
                PushEvent(new LogEvent("Internal Failure. Notify cannot be handled. ", e));
            }
        }


        private class ServerThreadData
        {
            public GatewayServer gatewayServer { get; private set; }
            public NetworkDevice device { get; private set; }


            public ServerThreadData(GatewayServer gatewayServer, NetworkDevice device)
            {
                this.gatewayServer = gatewayServer;
                this.device = device;

                (new Thread(new ThreadStart(ConnectionThread))).Start();
            }

            private void PushMessage(ClientConnection con, string message)
            {
                if ((message != null) && ((message = message.Trim()).Length > 0))
                    gatewayServer.PushEvent(new MessageEvent(device, con, message));
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
                            gatewayServer.PushEvent(new LogEvent(
                                            "Connection received from an ubiquitos-client device: '" +
                                            con.clientDevice.networkDeviceName + "' on '" + con.clientDevice.networkDeviceType + "'."));
                        }
                        else
                            throw new System.Exception("Couldn't estabilish connection to client.");

                        while (gatewayServer.running && con.connected)
                        {
                            StringBuilder builder = new StringBuilder();
                            byte[] data = new byte[1024];
                            int read;
                            while ((read = con.Read(data, 0, data.Length)) > 0)
                            {
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
                                        PushMessage(con, builder.ToString());
                                        builder.Length = 0;
                                    }
                                }

                                // Processes intermediate messages...
                                for (; i < last; ++i)
                                    PushMessage(con, msgs[i]);

                                // Processes the last chunk...
                                if (i == last)
                                {
                                    builder.Append(msgs[i]);
                                    if (lastByte == '\n')
                                    {
                                        PushMessage(con, builder.ToString());
                                        builder.Length = 0;
                                    }
                                }
                            }
                        }
                    }
                    catch (System.Exception e)
                    {
                        if (con != null)
                            con.Close();

                        gatewayServer.PushEvent(new LogEvent("Failed to handle ubiquitos-smartspace connection. ", e));
                    }
                }
            }
        }
    }
}
