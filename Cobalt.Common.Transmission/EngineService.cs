using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Cobalt.Common.Transmission;
using Cobalt.Common.Transmission.Messages;
using Grpc.Core;
using Grpc.Core.Interceptors;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Client;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Grpc.Server;

namespace Cobalt.Common.Transmission
{
    public class EngineService : IEngineService
    {
        private readonly Subject<AppSwitchMessage> _appSwitches =
            new Subject<AppSwitchMessage>();

        public async IAsyncEnumerable<AppSwitchMessage> AppSwitches(/*CallContext context = default*/)
        {
            await foreach (var x in _appSwitches
                .ToAsyncEnumerable()
                /*.WithCancellation(context.CancellationToken)*/)
                yield return x;
        }

        public void PushAppSwitch(AppSwitchMessage msg)
        {
            _appSwitches.OnNext(msg);
        }

    }
}