using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using Cobalt.Common.Communication;
using Cobalt.Common.Communication.Messages;
using Microsoft.Extensions.Logging;

namespace Cobalt.Engine.Services
{
    public class UsageService : IUsageService
    {
        private readonly Subject<UsageSwitch> _fgWinSwitches = new Subject<UsageSwitch>();
        private readonly ILogger<UsageService> _logger;

        public UsageService(ILogger<UsageService> logger)
        {
            _logger = logger;
        }

        public IAsyncEnumerable<UsageSwitch> ForegroundUsageSwitches()
        {
            // TODO todo check if method calls are logged by default, else log here
            return _fgWinSwitches.ToAsyncEnumerable();
        }

        public void PushUsageSwitch(UsageSwitch sw)
        {
            _fgWinSwitches.OnNext(sw);
        }
    }
}