using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using Cobalt.Common.Transmission.Messages;

namespace Cobalt.Common.Transmission
{
    public class EngineService : IEngineService
    {
        private readonly Subject<AppSwitchMessage> _appSwitches =
            new Subject<AppSwitchMessage>();

        public async IAsyncEnumerable<AppSwitchMessage> AppSwitches( /*CallContext context = default*/)
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