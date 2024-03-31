using DialogHostAvalonia;
using DynamicData;
using PeoplesTaskApp.DataTemplates;
using PeoplesTaskApp.Utils.Extensions;
using PeoplesTaskApp.Utils.Models;
using ReactiveUI;
using System;
using System.Collections.Concurrent;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace PeoplesTaskApp.Services
{
    /// <summary>
    /// Диалог с одним идентификатором может быть открыт только, если предыдущий был закрыт.
    /// Данный менеджер открывает диалог только если предыдущий был закрыт
    /// </summary>
    internal class DialogHostManager : DisposableBase, IDialogHostManager
    {
        private class DialogGroup : DisposableBase
        {
            public string Identifier { get; }
            public Subject<Unit> Notifier { get; } = new();
            public SemaphoreSlim Locker { get; } = new(1);
            public ConcurrentQueue<(DialogContentInfo contentInfo, TaskCompletionSource<object?> waiter)> Tasks { get; } = new();

            public DialogGroup(string identifier)
            {
                Identifier = identifier;
                Notifier.OnCompleteWith(DisposableOnDestroy);
            }
        }

        private readonly SourceCache<DialogGroup, string> _dialogQueues = new(x => x.Identifier);

        public DialogHostManager()
        {
            Disposable.Create(_dialogQueues.Clear).DisposeWith(DisposableOnDestroy);

            _dialogQueues.Connect()
                .SubscribeMany(group =>
                    group.Notifier
                        .ObserveOn(RxApp.MainThreadScheduler)
                        .Subscribe(async _ =>
                        {
                            using (await group.Locker.UseWaitAsync())
                            {
                                while (group.Tasks.TryDequeue(out var item))
                                    item.waiter.TrySetResult(await DialogHost.Show(item.contentInfo, group.Identifier));
                            }
                        }))
                .Subscribe();
        }

        public async Task<object?> ShowDialogAsync(string dialogIdentifier, DialogContentInfo contentInfo)
        {
            var group = new DialogGroup(dialogIdentifier);
            _dialogQueues.Edit(updator =>
            {
                var groupOptional = updator.Lookup(dialogIdentifier);
                if (groupOptional.HasValue)
                    group = groupOptional.Value;
                else
                    updator.AddOrUpdate(group);
            });

            var waiter = new TaskCompletionSource<object?>();
            group.Tasks.Enqueue((contentInfo, waiter));
            group.Notifier.OnNext(Unit.Default);

            return await waiter.Task;
        }
    }
}
