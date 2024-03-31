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

        private SelectablePersonViewModel? _selectedPerson;
        public SelectablePersonViewModel? SelectedPerson
        {
            get => _selectedPerson;
            set
            {
                // Setter должен вызываться из RxApp.MainThreadScheduler, по 2м причинам:
                // 1 RaiseAndSetIfChanged должен вызываться в потоке интерфейса,
                //      чтобы событие PropertyChanged также сгенерировалось в нём,
                //      т.к. все обработчике во View могут обрабатывать события только в одном потоке - в том, где они были созданы
                // 2 Persons меняется в главном потоке и не является потокобезопасной коллекцией,
                //      т.е. если изменение коллекции произойдёт в другом потоке во время выполнения метода Contains, то будет Exception
                if (_selectedPerson != value && (value is null || Persons.Contains(value)))
                    this.RaiseAndSetIfChanged(ref _selectedPerson, value);
            }
        }

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
        public Interaction<ReadOnlyPersonViewModel, bool> RemovePersonAllowRequests { get; } = new(RxApp.MainThreadScheduler);
        public ReactiveCommand<Unit, bool> RemovePerson { get; }

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
            _model.Persons
                .Connect()
                .Transform(person => new SelectablePersonViewModel(person))
                .SubscribeMany(personVM =>
                    personVM.WhenAnyValue(vm => vm.IsSelected)
                        .Where(selected => selected)
                        .ObserveOn(RxApp.MainThreadScheduler)
                        .Subscribe(_ => SelectedPerson = personVM))
                .DisposeMany()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out var personsROOC)
                .Subscribe()
                .DisposeWith(DisposableOnDestroy);

            Persons = personsROOC;

            this.WhenAnyValue(vm => vm.SelectedPerson)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(selectedPersonVM =>
                {
                    foreach (var personVM in Persons)
                        personVM.IsSelected = selectedPersonVM == personVM;
                })
                .DisposeWith(DisposableOnDestroy);

            AddPerson = ReactiveCommand.CreateFromTask(_ => Task.Run(AddPersonImplAsync));
            AddPerson.AddDefaultSubscriptions(DisposableOnDestroy);

            RemovePerson
                = ReactiveCommand.CreateFromTask(_ => Task.Run(RemovePersonImplAsync),
                    this.WhenAnyValue(vm => vm.SelectedPerson)
                        .Select(selectedPersonVM => selectedPersonVM is not null)
                        .ObserveOn(RxApp.MainThreadScheduler));
            RemovePerson.AddDefaultSubscriptions(DisposableOnDestroy);

            UpdatePerson
                = ReactiveCommand.CreateFromTask(_ => Task.Run(UpdatePersonImplAsync),
                    this.WhenAnyValue(vm => vm.SelectedPerson)
                        .Select(selectedPersonVM => selectedPersonVM is not null)
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

        private async Task<bool> RemovePersonImplAsync()
        {
            var selectedPerson = SelectedPerson;
            if (selectedPerson is null)
                return false;

            // Если подписки на RemovePersonAllowRequests нет, то ошибка автоматически прокинется через ErrorHandlers.UnhandledErrors
            var allow = await RemovePersonAllowRequests.Handle(selectedPerson);
            if (allow)
            {
                _model.RemovePerson(selectedPerson.Id);
                return true;
            }

            return false;
        }

        private async Task<bool> UpdatePersonImplAsync()
        {
            using (var selectedPersonForEdit = SelectedPerson?.Edit())
            {
                if (selectedPersonForEdit is null)
                    return false;

                // Если подписки на RemovePersonAllowRequests нет, то ошибка автоматически прокинется через ErrorHandlers.UnhandledErrors
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
