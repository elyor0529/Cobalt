using System.Collections.Generic;
using System.ServiceModel;
using Cobalt.Common.Transmission.Messages;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace Cobalt.Common.Transmission
{
    [ServiceContract]
    public interface IEngineService
    {
        IAsyncEnumerable<AppSwitchMessage> AppSwitches(/*CallContext context = default*/);
    }
}
