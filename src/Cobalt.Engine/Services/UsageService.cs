using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using Cobalt.Common.Communication;
using Cobalt.Common.Communication.Messages;

namespace Cobalt.Engine.Services
{
    public class UsageService : IUsageService
    {
        private readonly Subject<UsageSwitch> _fgWinSwitches = new Subject<UsageSwitch>();

        public IAsyncEnumerable<UsageSwitch> ForegroundUsageSwitches()
        {
            return _fgWinSwitches.ToAsyncEnumerable();
        }

        public void PushUsageSwitch(UsageSwitch sw)
        {
            _fgWinSwitches.OnNext(sw);
        }
    }
}