using PeoplesTaskApp.Utils.Extensions;
using PeoplesTaskApp.Utils.Services;
using ReactiveUI;
using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace PeoplesTaskApp.Extensions
{
    public static class ReactiveCommandExtensions
    {
        public static void AddDefaultSubscriptions<TParam, TResult>(this ReactiveCommand<TParam, TResult> command,
            CompositeDisposable disposeWith)
        {
            command.ThrownExceptions.Select(ErrorInteractions.UnhandledErrors.Handle).Switch().Subscribe().DisposeWith(disposeWith);
            Disposable.Create(() =>
                    // Dispose command only after it has been finished to remove ObjectDisposedException
                    command.IsExecuting.FirstOrNothingAsync(executing => !executing).Subscribe(_ => { }, () => command.Dispose()))
                .DisposeWith(disposeWith);
        }
    }
}
