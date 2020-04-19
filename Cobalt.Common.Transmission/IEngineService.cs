using System.Collections.Generic;
using System.ServiceModel;
using Cobalt.Common.Transmission.Messages;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace Cobalt.Common.Transmission
{
    [ServiceContract(Name = "NANI")]
    public interface IEngineService
    {
        [OperationContract]
        IAsyncEnumerable<AppSwitchMessage> AppSwitches(AppSwitchRequest req/*, CallContext context = default*/);

        [OperationContract]
        AppSwitchMessage Ping();
    }
}
