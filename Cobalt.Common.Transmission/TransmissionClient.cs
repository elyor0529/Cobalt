using System;
using System.Collections.Generic;
using System.Text;
using Grpc.Core;
using ProtoBuf.Grpc.Client;

namespace Cobalt.Common.Transmission
{
    public class TransmissionClient
    {
        private readonly Channel _channel;

        public TransmissionClient()
        {
            GrpcClientFactory.AllowUnencryptedHttp2 = true;
            _channel = new Channel(Constants.HostName, Constants.Port, Constants.ClientCredentials);
        }

        public IEngineService EngineService()
        {
            return _channel.CreateGrpcService<IEngineService>();
        }
    }
}
