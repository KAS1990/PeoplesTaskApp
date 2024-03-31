using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using PeoplesTaskApp.DataTemplates;
using PeoplesTaskApp.Models;
using PeoplesTaskApp.Services;
using PeoplesTaskApp.Utils.Extensions;
using PeoplesTaskApp.ViewModels;
using ReactiveUI;
using Splat;
using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Threading.Tasks;

namespace PeoplesTaskApp.Views;

public abstract class MainViewBase : CustomView<MainViewModel> { }
public sealed partial class MainView : MainViewBase
{
    public MainView()
    {
        InitializeComponent();
    }

    protected override void OnActivated(CompositeDisposable disposable)
    {
        NotNullViewModel.PersonsList.NewPersonRequests.RegisterHandler(NewPersonRequestsHandlerAsync).DisposeWith(disposable);
        NotNullViewModel.PersonsList.RemovePersonAllowRequests.RegisterHandler(RemovePersonAllowRequestsHandlerAsync).DisposeWith(disposable);
        NotNullViewModel.PersonsList.NewPersonDataRequests.RegisterHandler(NewPersonDataRequestsHandlerAsync).DisposeWith(disposable);
    }

    private async Task NewPersonDataRequestsHandlerAsync(IInteractionContext<ForEditingPersonViewModel, bool> context)
    {
        var dialog = new ModalDialogWindow()
        {
            Title = Langs.Resources.DialogTitleEditPerson,
            DataContext = context.Input
        };

        using (new CompositeDisposable
            {
                context.Input.Confirm.Subscribe(_ => dialog.Close(true)),
                context.Input.Discard.Subscribe(_ => dialog.Close(false))
            })
        {
            var saved
                = await dialog.ShowDialog<bool>((Application.Current!.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)!.MainWindow!);

            context.SetOutput(saved);
        }
    }

    private async Task RemovePersonAllowRequestsHandlerAsync(IInteractionContext<ReadOnlyPersonViewModel, bool> context)
    {
        var manager = Locator.Current.GetServiceOrThrow<IDialogHostManager>();

        var userChoice
            = await manager.ShowDialogAsync("MainDialogHost",
                    new DialogContentInfo("YesNoQuestionDialogTemplate",
                        string.Format(Langs.Resources.RemovePersonQuestionFormatString,
                            context.Input.Name,
                            context.Input.Surname)));

        context.SetOutput(userChoice?.ToString() == "Yes");
    }

    private async Task NewPersonRequestsHandlerAsync(IInteractionContext<Unit, Person?> context)
    {
        using (var viewModel = new ForEditingPersonViewModel(new Person()))
        {
            var dialog = new ModalDialogWindow()
            {
                Title = Langs.Resources.DialogTitleAddPerson,

                DataContext = viewModel
            };

            using (new CompositeDisposable
            {
                viewModel.Confirm.Subscribe(_ => dialog.Close(true)),
                viewModel.Discard.Subscribe(_ => dialog.Close(false))
            })
            {
                var saved
                    = await dialog.ShowDialog<bool>((Application.Current!.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)!.MainWindow!);

                context.SetOutput(saved ? viewModel.Model : null);
            }
        }
    }

    public void PersonsDataGrid_DoubleTapped(object? sender, TappedEventArgs e)
    {
        var point = e.GetPosition(PersonsDataGrid);
        var controlUnderCursor = PersonsDataGrid.InputHitTest(point) as Control;
        if (controlUnderCursor is null)
            return;

        var selectedVM = controlUnderCursor.FindLogicalAncestorOfType<DataGridRow>()?.DataContext as SelectablePersonViewModel;
        if (selectedVM is not null)
            ViewModel!.PersonsList.UpdatePerson.Execute().Subscribe();
    }
}
