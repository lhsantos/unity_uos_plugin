using System.Collections.Generic;
using System.Reflection;


namespace UOS
{
    public class ReflectionServiceCaller
    {
        private UnityGateway gateway;
        private Logger logger = null;

        public ReflectionServiceCaller(uOSSettings settings, UnityGateway gateway)
        {
            this.gateway = gateway;
            this.logger = gateway.logger;
        }

        public Response CallService(object instance, Call call, CallContext context)
        {
            MethodInfo method = FindMethod(call, instance);

            if (method != null)
            {
                logger.Log(
                    "Calling service '" + call.service + "' of driver '" + call.driver + "' on instance '" + call.instanceId + "'");

                HandleStreamCall(call, context);

                Response response = new Response();
                method.Invoke(instance, new object[] { call, response, context });

                logger.Log("Finished service call.");
                return response;
            }
            else
                throw new System.Exception(
                    "No Service Implementation found for service '" + call.service +
                    "' on driver '" + call.driver + "' with id '" + call.instanceId + "'.");
        }

        private MethodInfo FindMethod(Call serviceCall, object instanceDriver)
        {
            string serviceName = serviceCall.service;

            foreach (var m in instanceDriver.GetType().GetMethods())
            {
                if (m.Name.Equals(serviceName, System.StringComparison.InvariantCultureIgnoreCase))
                    return m;
            }

            return null;
        }

        private void HandleStreamCall(Call serviceCall, CallContext messageContext)
        {
            if (serviceCall.serviceType == ServiceType.STREAM)
            {
                NetworkDevice networkDevice = messageContext.callerNetworkDevice;

                string host = Util.GetHost(networkDevice.networkDeviceName);
                for (int i = 0; i < serviceCall.channels; i++)
                {
                    ClientConnection con = gateway.OpenActiveConnection(host + ":" + serviceCall.channelIDs[i], serviceCall.channelType);
                    messageContext.AddConnection(con);
                }
            }
        }
    }
}
