using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using PeoplesTaskApp.Utils.Extensions;
using PeoplesTaskApp.ViewModels;
using ReactiveUI;
using Splat;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace PeoplesTaskApp.Views
{
    public abstract class CustomView<TViewModel> : UserControl, IViewFor<TViewModel>, ICanActivate, IEnableLogger
        where TViewModel : ViewModelBase
    {
        #region ViewModel Property

        public static readonly DirectProperty<CustomView<TViewModel>, TViewModel?> ViewModelProperty =
            AvaloniaProperty.RegisterDirect<CustomView<TViewModel>, TViewModel?>(
                nameof(ViewModel),
                o => o.ViewModel,
                (o, v) => o.ViewModel = v,
                defaultBindingMode: BindingMode.OneWay);

        private TViewModel? _viewModel;

        public TViewModel? ViewModel
        {
            get => _viewModel;
            set
            {
                if (SetAndRaise(ViewModelProperty, ref _viewModel, value))
                    DataContext = value;
            }
        }

        object? IViewFor.ViewModel { get => ViewModel; set => ViewModel = (TViewModel?)value; }

        public TViewModel NotNullViewModel =>
            _viewModel ?? throw new ArgumentNullException($"{GetType().Name} {NotNullViewModel} is null");

        #endregion

        //
        // Summary:
        //     Gets a observable which is triggered when the ViewModel is activated.
        public IObservable<Unit> Activated { get; }

        //
        // Summary:
        //     Gets a obervable which is triggered when the ViewModel is deactivated.
        public IObservable<Unit> Deactivated { get; }

        protected CustomView(bool isAsyncActivatedFunc = false)
        {
            Activated
                = Observable.FromEventPattern<RoutedEventArgs>(
                        h => Loaded += h,
                        h => Loaded -= h)
                    .ToUnit()
                    .CombineLatest(ViewModelProperty.Changed.Where(args => args.NewValue.Value is not null), (_, _) => Unit.Default)
                    .Publish()
                    .RefCount();
            Deactivated
                = Observable.FromEventPattern<RoutedEventArgs>(
                        h => Unloaded += h,
                        h => Unloaded -= h)
                    .ToUnit()
                    .Publish()
                    .RefCount();

            if (isAsyncActivatedFunc)
            {
                this.WhenActivated(async disposable =>
                {
                    await OnActivatedAsync(disposable);
                    Disposable.Create(OnDeactivated).DisposeWith(disposable);
                });
            }
            else
            {
                this.WhenActivated(disposable =>
                {
                    OnActivated(disposable);
                    Disposable.Create(OnDeactivated).DisposeWith(disposable);
                });
            }
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            if (DataContext is TViewModel viewModel)
                ViewModel = viewModel;
        }

        protected virtual Task OnActivatedAsync(CompositeDisposable disposable) => Task.CompletedTask;

        protected virtual void OnActivated(CompositeDisposable disposable) { }

        protected virtual void OnDeactivated() { }
    }
}
