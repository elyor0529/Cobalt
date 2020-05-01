using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using Cobalt.Common.Communication;
using Cobalt.Common.Communication.Messages;

namespace Cobalt.Engine
{
    public class EngineService : IEngineService
    {
        private readonly Subject<ForegroundWindowSwitch> _fgWinSwitches = new Subject<ForegroundWindowSwitch>();

        public IAsyncEnumerable<ForegroundWindowSwitch> ForegroundWindowSwitches()
        {
            return _fgWinSwitches.ToAsyncEnumerable();
        }

        public void PushForegroundWindowSwitch(ForegroundWindowSwitch sw)
        {
            _fgWinSwitches.OnNext(sw);
        }
    }
}