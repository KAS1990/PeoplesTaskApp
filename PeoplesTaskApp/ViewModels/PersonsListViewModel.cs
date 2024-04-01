using DynamicData;
using DynamicData.Kernel;
using PeoplesTaskApp.Extensions;
using PeoplesTaskApp.Models;
using PeoplesTaskApp.Utils.Extensions;
using PeoplesTaskApp.Utils.Models;
using PeoplesTaskApp.Utils.Services;
using PeoplesTaskApp.Utils.Services.DataSources;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace PeoplesTaskApp.ViewModels
{
    public class PersonsListViewModel : ModelHolderViewModel<PersonsList>
    {
        #region Persons

        /// <summary>
        /// Все операции с этой коллекцией должны производится в главном потоке, т.к. коллекция не потокобезопасная
        /// </summary>
        public ReadOnlyObservableCollection<SelectablePersonViewModel> Persons { get; }

        public IObservableCache<SelectablePersonViewModel, Guid> PersonsDynamicCache { get; }

        #endregion

        #region Loading and saving persons

        // Эти свойства также должны устнанвливаться в потоке интерфейса

        [Reactive] public bool IsLoadingData { get; private set; }
        [Reactive] public bool IsSavingData { get; private set; }

        #endregion

        #region Persons commands

        /// <summary>
        // Запрос на добавление данных. Если вернулось null, то добавление не требуется.
        // Можно сделать кортеж (bool, Person), но для данного примера я решил сделать по-проще
        /// </summary>
        public Interaction<Unit, Person?> NewPersonRequests { get; } = new(RxApp.MainThreadScheduler);
        public ReactiveCommand<Unit, bool> AddPerson { get; }

        /// <summary>
        /// true - можно удалять. Это нужно, чтобы перед удалением показать сообщение пользователю
        /// </summary>
        public Interaction<IReadOnlyList<ReadOnlyPersonViewModel>, bool> RemovePersonsAllowRequests { get; } = new(RxApp.MainThreadScheduler);
        public ReactiveCommand<Unit, bool> RemoveSelectedPersons { get; }

        /// <summary>
        /// true - нужно обновить. Это нужно, чтобы показать диалог изменения данных
        /// Новые значения должны быть в переданной view model'и, т.к. изначально в ней будет копия данных из списка,
        /// т.е. менять эту view model можно, не боясь повредить что-то в списке
        /// </summary>
        public Interaction<ForEditingPersonViewModel, bool> NewPersonDataRequests { get; } = new(RxApp.MainThreadScheduler);
        public ReactiveCommand<Unit, bool> UpdatePerson { get; }

        #endregion

        public ReactiveCommand<Unit, Unit> LoadData { get; }

        public ReactiveCommand<Unit, Unit> SaveData { get; }

        public PersonsListViewModel(PersonsList model) : base(model)
        {
            PersonsDynamicCache
                = _model.Persons
                    .Connect()
                    .Transform(person => new SelectablePersonViewModel(person))
                    .DisposeMany()
                    .AsObservableCache()
                    .DisposeWith(DisposableOnDestroy);

            PersonsDynamicCache.Connect()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out var personsROOC)
                .Subscribe()
                .DisposeWith(DisposableOnDestroy);

            Persons = personsROOC;

            AddPerson = ReactiveCommand.CreateFromTask(_ => Task.Run(AddPersonImplAsync));
            AddPerson.AddDefaultSubscriptions(DisposableOnDestroy);

            var onSelectedChangedPersonsCollection
                = PersonsDynamicCache.Connect()
                    .AutoRefreshOnObservable(p => p.WhenAnyValue(vm => vm.IsSelected))
                    .ToCollection()
                    .Publish();

            RemoveSelectedPersons
                = ReactiveCommand.CreateFromTask(_ => Task.Run(RemovePersonsImplAsync),
                    onSelectedChangedPersonsCollection.Select(persons => persons.Any(p => p.IsSelected))
                        .ObserveOn(RxApp.MainThreadScheduler));
            RemoveSelectedPersons.AddDefaultSubscriptions(DisposableOnDestroy);

            UpdatePerson
                = ReactiveCommand.CreateFromTask(_ => Task.Run(UpdatePersonImplAsync),
                    onSelectedChangedPersonsCollection.Select(persons => persons.Count(p => p.IsSelected) == 1)
                        .ObserveOn(RxApp.MainThreadScheduler));
            UpdatePerson.AddDefaultSubscriptions(DisposableOnDestroy);

            // Загружаем данные в фоновом потоке
            LoadData
                = ReactiveCommand.CreateFromTask(_ => Task.Run(LoadDataImplAsync, _),
                    this.WhenAnyValue(x => x.IsLoadingData).Select(processing => !processing));
            LoadData.AddDefaultSubscriptions(DisposableOnDestroy);

            // Сохраняем данные в фоновом потоке
            SaveData
                = ReactiveCommand.CreateFromTask(_ => Task.Run(SaveDataImplAsync, _),
                    this.WhenAnyValue(x => x.IsSavingData).Select(processing => !processing));
            SaveData.AddDefaultSubscriptions(DisposableOnDestroy);

            onSelectedChangedPersonsCollection.Connect().DisposeWith(DisposableOnDestroy);
        }

        private async Task<bool> AddPersonImplAsync()
        {
            // Если подписки на NewPersonRequests нет, то ошибка автоматически прокинется через ErrorHandlers.UnhandledErrors
            var newModel = await NewPersonRequests.Handle(Unit.Default);
            if (newModel is not null)
            {
                _model.AddPerson(newModel);
                return true;
            }

            return false;
        }

        private async Task<bool> RemovePersonsImplAsync()
        {
            var selectedPersons = Persons.Where(p => p.IsSelected).ToList();
            if (selectedPersons.Count == 0)
                return false;

            // Если подписки на RemovePersonsAllowRequests нет, то ошибка автоматически прокинется через ErrorHandlers.UnhandledErrors
            var allow = await RemovePersonsAllowRequests.Handle(selectedPersons);
            if (allow)
            {
                _model.RemovePersons(selectedPersons.Select(p => p.Id));
                return true;
            }

            return false;
        }

        private async Task<bool> UpdatePersonImplAsync()
        {
            var selectedPerson = Persons.FirstOrDefault(p => p.IsSelected);
            if (selectedPerson is null)
                return false;

            using (var selectedPersonForEdit = selectedPerson.Edit())
            {
                if (selectedPersonForEdit is null)
                    return false;

                // Если подписки на RemovePersonsAllowRequests нет, то ошибка автоматически прокинется через ErrorHandlers.UnhandledErrors
                var allow = await NewPersonDataRequests.Handle(selectedPersonForEdit);
                if (allow)
                {
                    _model.UpdatePerson(selectedPersonForEdit.Model);
                    return true;
                }

                return false;
            }
        }

        public async Task LoadDataImplAsync()
        {
            var dataLoaderService
                = Locator.Current.GetService<IDataService<Person[]>>(AppConfiguration.DataFileServiceContractName)
                    ?? throw new NullReferenceException("Data service for loading data is not found");

            var progressSubscription = new SingleAssignmentDisposable();
            progressSubscription.Disposable
                = dataLoaderService.SavingProgress
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(item =>
                    {
                        switch (item.Step)
                        {
                            case SaveLoadStepType.NotStarted:
                                IsLoadingData = false;
                                break;
                            case SaveLoadStepType.End:
                                IsLoadingData = false;
                                progressSubscription.Dispose();
                                break;
                            case SaveLoadStepType.Start:
                            case SaveLoadStepType.Progress:
                                IsLoadingData = true;
                                break;
                        }

                        CheckException(item, progressSubscription);
                    });
            try
            {
                _model.Load((await dataLoaderService.LoadAsync())
                            .Select(p => p.Id == Guid.Empty ? new Person(Guid.NewGuid(), p) : p));
            }
            catch
            { }
        }

        public async Task SaveDataImplAsync()
        {
            var dataLoaderService = Locator.Current.GetServiceOrThrow<IDataService<Person[]>>(AppConfiguration.DataFileServiceContractName);

            var progressSubscription = new SingleAssignmentDisposable();
            progressSubscription.Disposable
                = dataLoaderService.SavingProgress
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(item =>
                    {
                        switch (item.Step)
                        {
                            case SaveLoadStepType.NotStarted:
                                IsSavingData = false;
                                break;
                            case SaveLoadStepType.End:
                                IsSavingData = false;
                                progressSubscription.Dispose();
                                break;
                            case SaveLoadStepType.Start:
                            case SaveLoadStepType.Progress:
                                IsSavingData = true;
                                break;
                        }

                        CheckException(item, progressSubscription);
                    });
            try
            {
                // IObservableCache.Items возвращает массив, поэтому AsArray ничего по факту не будет делать
                await dataLoaderService.SaveAsync(_model.Persons.Items.AsArray());
            }
            catch
            { }
        }

        private static void CheckException(DataSaveLoadProgressItem item, SingleAssignmentDisposable progressSubscription)
        {
            if (item.Exception is not null)
            {
                ErrorInteractions.UnhandledErrors.Handle(item.Exception).Subscribe();
                progressSubscription.Dispose();
            }
        }
    }
}
