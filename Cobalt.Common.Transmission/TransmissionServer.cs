using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Grpc.Core;
using ProtoBuf.Grpc.Client;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Grpc.Server;

namespace Cobalt.Common.Transmission
{
    public class TransmissionServer
    {
        private readonly Server _server;
        private readonly EngineService _engineSvc;

        public TransmissionServer(EngineService engineSvc)
        {
            _engineSvc = engineSvc;
            _server = new Server
            {
                Ports = { new ServerPort(Constants.HostName, Constants.Port, Constants.ServerCredentials) },
            };
            _server.Services.AddCodeFirst<IEngineService>(_engineSvc);
        }

        public void StartServer()
        {
            _server.Start();
        }
    }
}
