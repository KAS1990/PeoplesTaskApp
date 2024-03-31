using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using PeoplesTaskApp.Models;
using PeoplesTaskApp.Services;
using PeoplesTaskApp.Services.DataSources;
using PeoplesTaskApp.Utils.Extensions;
using PeoplesTaskApp.Utils.Services;
using PeoplesTaskApp.Utils.Services.DataSources;
using PeoplesTaskApp.ViewModels;
using PeoplesTaskApp.Views;
using ReactiveUI;
using Splat;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace PeoplesTaskApp;

public partial class App : Application
{
    private bool _allowClose = false;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            throw new ArgumentException("Invalid ApplicationLifetime value");

        if (CultureInfo.InstalledUICulture.IetfLanguageTag == "ru-RU")
            Langs.Resources.Culture = CultureInfo.InstalledUICulture;
        else
            Langs.Resources.Culture = new CultureInfo("en-US");

        // Читаем настройки из конфигурации
        var builder = Host.CreateApplicationBuilder(desktop.Args);
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        var settings = new AppConfiguration();
        builder.Configuration.GetSection("Settings").Bind(settings);

        RxApp.TaskpoolScheduler
            = TaskPoolScheduler.Default
                .Catch<Exception>(ex =>
                {
                    ErrorInteractions.TaskPoolSchedulerErrors.Handle(ex).Subscribe();
                    return true;
                })
                .DisableOptimizations(typeof(ISchedulerLongRunning));

        TaskScheduler.UnobservedTaskException += (s, e) => ErrorInteractions.TaskPoolSchedulerErrors.Handle(e.Exception).Subscribe();

        RxApp.DefaultExceptionHandler = Observer.Create<Exception>(ex =>
        {
            if (Debugger.IsAttached)
                Debugger.Break();

            ErrorInteractions.UnhandledFatalErrors.Handle(ex).Subscribe();
        });

        var locator = Locator.CurrentMutable;
        switch (settings.DataFileFormat)
        {
            case "json":
                // Регистрируем дата провайдер чтобы его везде дальше использовать
                locator.RegisterLazySingleton<IDataService<Person[]>>(() =>
                    new JsonConverter<Person[]>(new FileDataSource(settings.DataFullFilePath)), AppConfiguration.DataFileServiceContractName);
                break;

            default:
                throw new ArgumentException($"Invalid data file format: {settings.DataFileFormat}");
        }

        locator.RegisterConstant<IDialogHostManager>(new DialogHostManager());

        // Нужно зарегистрировать view model'и, чтобы потом ViewModelViewHost смог создавать для них View
        locator.RegisterViewsForViewModels(Assembly.GetExecutingAssembly());

        var mainWindow = new MainWindow();
        desktop.MainWindow = mainWindow;

        Observable.Create<MainViewModel>(obs =>
            {
                var mainVM = new MainViewModel();
                obs.OnNext(mainVM);    // Модель создадим в фоновом потоке
                obs.OnCompleted();

                mainVM.PersonsList.LoadData.Execute().Subscribe();  // Загружаем данные

                return Disposable.Empty;
            })
            .SubscribeOn(RxApp.TaskpoolScheduler)
            .ObserveOn(RxApp.MainThreadScheduler)   // MainWindow нужно в потоке интерфейса задавать
            .Subscribe(vm => mainWindow.DataContext = vm);

        desktop.ShutdownRequested += Desktop_ShutdownRequested;

        base.OnFrameworkInitializationCompleted();
    }

    private void Desktop_ShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        if (_allowClose)
            return;

        e.Cancel = true;
        _allowClose = false;

        var desktop = (sender as IClassicDesktopStyleApplicationLifetime)!;
        var mainVM = (desktop.MainWindow!.DataContext as MainViewModel)!;

        mainVM.PersonsList
            .SaveData
            .CanExecute
            .FirstOrNothingAsync(canExecute => canExecute)
            .Timeout(TimeSpan.FromSeconds(3))   // Чтобы не зависли в случае ошибки
            .Select(_ =>
                mainVM.PersonsList
                    .SaveData
                    .Execute()
                    .Select(_ => mainVM.PersonsList.WhenAnyValue(vm => vm.IsSavingData).FirstOrDefaultAsync(processing => !processing))
                    .Switch())
            .Switch()
            .Finally(() =>
            {
                _allowClose = true;
                var res = desktop.TryShutdown();
            })
            .Subscribe();
    }
}
