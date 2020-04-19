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
        private readonly BehaviorSubject<AppSwitchMessage> _appSwitches =
            new BehaviorSubject<AppSwitchMessage>(new AppSwitchMessage());

        public async IAsyncEnumerable<AppSwitchMessage> AppSwitches(AppSwitchRequest req/*, CallContext context = default*/)
        {
            await foreach (var x in _appSwitches
                .ToAsyncEnumerable()
                /*.WithCancellation(context.CancellationToken)*/)
                yield return x;
        }

        public AppSwitchMessage Ping()
        {
            return new AppSwitchMessage();
        }

        public void PushAppSwitch(AppSwitchMessage msg)
        {
            _appSwitches.OnNext(msg);
        }

    }
}