using PeoplesTaskApp.Models;
using ReactiveUI;
using System;
using System.Collections;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace PeoplesTaskApp.ViewModels
{
    public class ReadOnlyPersonViewModel : ModelHolderViewModel<Person>
    {
        public Guid Id => _model.Id;

        public string Name { get => _model.Name; }

        public string Surname { get => _model.Surname; }

        public int Age { get => _model.Age; }

        public string? City { get => _model.City; }
                
        public ReadOnlyPersonViewModel(Person model) : base(model)
        {
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
        }

        public ForEditingPersonViewModel Edit() => new(_model.Clone());
    }
}
