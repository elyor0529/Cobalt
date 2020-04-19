using System.Collections.Generic;
using System.ServiceModel;
using Cobalt.Common.Transmission.Messages;

namespace Cobalt.Common.Transmission
{
    [ServiceContract]
    public interface IEngineService
    {
        IAsyncEnumerable<AppSwitchMessage> AppSwitches( /*CallContext context = default*/);
    }
}