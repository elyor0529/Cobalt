using Grpc.Core;
using ProtoBuf.Grpc.Server;

namespace Cobalt.Common.Transmission
{
    public class TransmissionServer
    {
        private readonly EngineService _engineSvc;
        private readonly Server _server;

        public TransmissionServer(EngineService engineSvc)
        {
            _engineSvc = engineSvc;
            _server = new Server
            {
                Ports = {new ServerPort(Constants.HostName, Constants.Port, Constants.ServerCredentials)}
            };
            _server.Services.AddCodeFirst<IEngineService>(_engineSvc);
        }

        public void StartServer()
        {
            _server.Start();
        }
    }
}