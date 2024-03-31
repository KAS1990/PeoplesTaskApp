using PeoplesTaskApp.Models;
using ReactiveUI;
using System;
using System.Collections;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace PeoplesTaskApp.ViewModels
{
    public class ForEditingPersonViewModel : ModelHolderViewModel<Person>, INotifyDataErrorInfo
    {

        public Guid Id => _model.Id;

        private readonly Person _lastSavedValue;
        public Person Model => _model;

        public string Name { get => _model.Name; set => _model.Name = value; }

        public string Surname { get => _model.Surname; set => _model.Surname = value; }

        public int Age { get => _model.Age; set => _model.Age = value; }

        public string? City { get => _model.City; set => _model.City = value; }

        #region INotifyDataErrorInfo implementation

        public bool HasErrors => _model.HasErrors;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public IEnumerable GetErrors(string? propertyName) => _model.GetErrors(propertyName);

        #endregion

        /// <summary>
        /// Команда доступна только, если нет ошибок.
        /// Она ничего не делает и нужна для того, чтобы её можно было привязать к кнопке Отмена и определить потом,
        /// что была нажата именно эта кнопка
        /// </summary>
        public ReactiveCommand<Unit, Unit> Confirm { get; }

        /// <summary>
        /// Команда ничего не делает, она нужна для того, чтобы её можно было привязать к кнопке Отмена и определить потом,
        /// что была нажата именно эта кнопка
        /// </summary>
        public ReactiveCommand<Unit, Unit> Discard { get; }

        public ForEditingPersonViewModel(Person model) : base(model)
        {
            _lastSavedValue = _model.Clone();

            // Это view model, события PropertyChanging(ed) будут обрабатываться во View, которые могут это делать только в главном потоке,
            // но свойства модели и Changing/Changed могут генерироваться в любом потоке, чтобы вынести часть операций в другой поток,
            // поэтому ObserveOn(RxApp.MainThreadScheduler) обязателен
            _model.Changing
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(args => this.RaisePropertyChanging(args.PropertyName))
                .DisposeWith(DisposableOnDestroy);
            _model.Changed
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(args => this.RaisePropertyChanged(args.PropertyName))
                .DisposeWith(DisposableOnDestroy);

            // Событие ErrorsChanged должно вызываться в главном потоке,
            // т.к. обрабатываться оног будет в интерфейсе
            var errorChangedProducer
                = Observable.FromEventPattern<DataErrorsChangedEventArgs>(h => _model.ErrorsChanged += h, h => _model.ErrorsChanged -= h)
                    .Publish()
                    .RefCount();

            errorChangedProducer.ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(h => ErrorsChanged?.Invoke(this, h.EventArgs))
                .DisposeWith(DisposableOnDestroy);

            Confirm
                = ReactiveCommand.Create(() => { },
                        errorChangedProducer.Select(_ => !HasErrors)
                            .StartWith(!HasErrors)
                            .CombineLatest(_model.Changed.Select(_ => !_model.FieldsValuesEqual(_lastSavedValue)),
                                (_, haveChanges) => haveChanges && !HasErrors)
                            .ObserveOn(RxApp.MainThreadScheduler));
            Discard = ReactiveCommand.Create(() => { });
        }
    }
}
