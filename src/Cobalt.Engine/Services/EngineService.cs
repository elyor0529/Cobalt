using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using Cobalt.Common.Communication;
using Cobalt.Common.Communication.Messages;

namespace Cobalt.Engine.Services
{
    public class EngineService : IEngineService
    {
        private readonly Subject<UsageSwitch> _fgWinSwitches = new Subject<UsageSwitch>();

        public IAsyncEnumerable<UsageSwitch> UsageSwitches()
        {
            return _fgWinSwitches.ToAsyncEnumerable();
        }

        public void PushUsageSwitch(UsageSwitch sw)
        {
            _fgWinSwitches.OnNext(sw);
        }
    }
}