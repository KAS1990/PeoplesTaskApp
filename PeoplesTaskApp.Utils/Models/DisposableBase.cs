using Splat;
using System.Reactive.Disposables;

namespace PeoplesTaskApp.Utils.Models
{
    public abstract class DisposableBase : IDisposable, ICancelable, IEnableLogger
    {
        public bool IsDisposed => DisposableOnDestroy.Count == 0;

        public CompositeDisposable DisposableOnDestroy { get; } = [];

        protected DisposableBase() => Disposable.Empty.DisposeWith(DisposableOnDestroy);

        ~DisposableBase() => Dispose(false);

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
