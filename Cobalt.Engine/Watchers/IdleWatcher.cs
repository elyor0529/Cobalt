using System;
using System.Reactive.Subjects;

namespace Cobalt.Engine.Watchers
{

    public class IdleWatcher : Watcher<DateTimeOffset>
    {
        public IdleWatcher()
        {
        }

        public override void Dispose()
        {
            throw new NotImplementedException();
        }

        public override void Watch()
        {
            throw new NotImplementedException();
        }
    }
}
