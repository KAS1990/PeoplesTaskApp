using Avalonia.Controls;
using PeoplesTaskApp.DataTemplates;
using PeoplesTaskApp.Services;
using PeoplesTaskApp.Utils.Extensions;
using PeoplesTaskApp.Utils.Services;
using Splat;
using System.Diagnostics;
using System.Reactive;

namespace PeoplesTaskApp.Views;

public partial class MainWindow : Window
{
    private const string MainDialogHostIdentifier = "MainDialogHost";

    public MainWindow()
    {
        InitializeComponent();

        ErrorInteractions.TaskPoolSchedulerErrors.RegisterHandler(async interaction =>
        {
            var ex = interaction.Input;

            LogHost.Default.Error(ex, "Task pool scheduler error exception");

            var exceptionMessages = ex.GetAllMessagesAsList();
            var exceptionStackTraces = ex.GetAllStackTraceAsList();

            var manager = Locator.Current.GetServiceOrThrow<IDialogHostManager>();
            await manager.ShowDialogAsync(MainDialogHostIdentifier,
                    new DialogContentInfo("ErrorDialogTemplate",
                        $"Task pool scheduler error exception\n{exceptionMessages}\n{exceptionStackTraces}"));

            if (Debugger.IsAttached)
                Debugger.Break();

            interaction.SetOutput(Unit.Default);
        });

        ErrorInteractions.UnhandledFatalErrors.RegisterHandler(async interaction =>
        {
            var ex = interaction.Input;

            LogHost.Default.Error(ex, "Fatal exception dialog");

            var exceptionMessages = ex.GetAllMessagesAsList();
            var exceptionStackTraces = ex.GetAllStackTraceAsList();

            var manager = Locator.Current.GetServiceOrThrow<IDialogHostManager>();
            await manager.ShowDialogAsync(MainDialogHostIdentifier,
                    new DialogContentInfo("ErrorDialogTemplate", $"Fatal exception dialog\n{exceptionMessages}\n{exceptionStackTraces}"));

            if (Debugger.IsAttached)
                Debugger.Break();

            interaction.SetOutput(Unit.Default);
        });

        ErrorInteractions.UnhandledErrors.RegisterHandler(async interaction =>
        {
            var ex = interaction.Input;

            LogHost.Default.Error(ex, "Error exception dialog");

            var exceptionMessages = ex.GetAllMessagesAsList();
            var exceptionStackTraces = ex.GetAllStackTraceAsList();

            var manager = Locator.Current.GetServiceOrThrow<IDialogHostManager>();
            await manager.ShowDialogAsync(MainDialogHostIdentifier,
                    new DialogContentInfo("ErrorDialogTemplate", $"Error exception dialog\n{exceptionMessages}\n{exceptionStackTraces}"));

            if (Debugger.IsAttached)
                Debugger.Break();

            interaction.SetOutput(Unit.Default);
        });
    }
}
