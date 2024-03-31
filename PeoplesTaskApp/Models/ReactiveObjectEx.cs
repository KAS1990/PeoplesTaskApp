using ReactiveUI;
using Splat;
using System;
using System.Reactive.Disposables;

namespace PeoplesTaskApp.Models
{
    public class ReactiveObjectEx : ReactiveObject, IDisposable, ICancelable, IEnableLogger
    {
        public bool IsDisposed => DisposableOnDestroy.Count == 0;

        public CompositeDisposable DisposableOnDestroy { get; } = [];

        protected ReactiveObjectEx() => Disposable.Empty.DisposeWith(DisposableOnDestroy);

        ~ReactiveObjectEx() => Dispose(false);

        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            try
            {
                DisposableOnDestroy.Clear();
            }
            catch (Exception ex)
            {
                this.Log().Error(ex, $"{ToString()} Error on Dispose({disposing})");
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
