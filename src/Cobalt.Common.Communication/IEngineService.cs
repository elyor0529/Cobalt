using System.Collections.Generic;
using System.ServiceModel;
using Cobalt.Common.Communication.Messages;

namespace Cobalt.Common.Communication
{
    [ServiceContract]
    public interface IEngineService
    {
        [OperationContract]
        public IAsyncEnumerable<UsageSwitch> UsageSwitches();
    }
}