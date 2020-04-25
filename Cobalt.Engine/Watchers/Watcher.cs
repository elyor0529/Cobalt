using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Text;

namespace Cobalt.Engine.Watchers
{
    public interface IWatcher<out T> : IObservable<T>, IDisposable
    {
        void Watch();
    }

    public abstract class Watcher<T> : IWatcher<T>
    {
        protected readonly Subject<T> Events = new Subject<T>();

        public abstract void Watch();
        public IDisposable Subscribe(IObserver<T> observer) => Events.Subscribe(observer);

        public abstract void Dispose();
    }
}
