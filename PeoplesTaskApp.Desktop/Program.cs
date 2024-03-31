using Avalonia;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using PeoplesTaskApp.Models;
using PeoplesTaskApp.Services.DataSources;
using Splat;
using System;
using System.Diagnostics;

namespace PeoplesTaskApp.Desktop;

internal class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        LogImpl? logImpl = null;
        try
        {
            // Читаем настройки из конфигурации
            var builder = Host.CreateApplicationBuilder(args);
            builder.Configuration.Sources.Clear();
            builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            var settings = new AppConfiguration();
            builder.Configuration.GetSection("Settings").Bind(settings);

            // Использую для регистрации лога Splat, т.к. на моём предыдущем проекте использовался именно он и код лога у меня уже есть.
            // Не нашёл подобного функционала в локаторе Avalonia, но если нужно, то можно написать свой Splat.LogHost
            // Везде далее также буду использовать этот локатор для едиообразия, но можно и AvalonLocator использовать без проблем
            var locator = Locator.CurrentMutable;

            logImpl = new LogImpl(new FileDataSource(string.Format(settings.LogFullFilePathTemplate, DateTime.Now)))
            {
                Level = Debugger.IsAttached ? LogLevel.Debug : LogLevel.Info
            };
            locator.RegisterConstant<ILogger>(logImpl);

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            LogHost.Default.Error(ex, "Fatal app crash");

            if (Debugger.IsAttached)
                Debugger.Break();
        }
        finally
        {
            logImpl?.LastItemGenerated();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();
}
