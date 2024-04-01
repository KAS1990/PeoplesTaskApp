using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.LogicalTree;
using DynamicData;
using PeoplesTaskApp.DataTemplates;
using PeoplesTaskApp.Models;
using PeoplesTaskApp.Services;
using PeoplesTaskApp.Utils.Extensions;
using PeoplesTaskApp.ViewModels;
using ReactiveUI;
using Splat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
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
        NotNullViewModel.PersonsList
            .RemovePersonsAllowRequests
            .RegisterHandler(RemovePersonAllowRequestsHandlerAsync)
            .DisposeWith(disposable);
        NotNullViewModel.PersonsList.NewPersonDataRequests.RegisterHandler(NewPersonDataRequestsHandlerAsync).DisposeWith(disposable);

        NotNullViewModel.PersonsList
            .PersonsDynamicCache
            .Connect()
            .AutoRefreshOnObservable(p => p.WhenAnyValue(vm => vm.IsSelected))
            .Filter(p => p.IsSelected)
            .ObserveOn(RxApp.MainThreadScheduler)
            .OnItemAdded(p =>
            {
                if (!PersonsDataGrid.SelectedItems.Contains(p))
                    PersonsDataGrid.SelectedItems.Add(p);
            })
            .OnItemRemoved(p => PersonsDataGrid.SelectedItems.Remove(p))
            .Subscribe()
            .DisposeWith(disposable);
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

    private async Task RemovePersonAllowRequestsHandlerAsync(IInteractionContext<IReadOnlyList<ReadOnlyPersonViewModel>, bool> context)
    {
        var manager = Locator.Current.GetServiceOrThrow<IDialogHostManager>();

        var userChoice
            = await manager.ShowDialogAsync("MainDialogHost",
                    new DialogContentInfo("YesNoQuestionDialogTemplate",
                        string.Format(Langs.Resources.RemovePersonQuestionFormatString,
                            string.Join("\n", context.Input.Select(p => $"{p.Name} {p.Surname}")))));

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
        if (PersonsDataGrid.InputHitTest(point) is not Control controlUnderCursor
            || controlUnderCursor.FindLogicalAncestorOfType<DataGridRow>()?.DataContext is not SelectablePersonViewModel)
        {
            return;
        }

        ViewModel!.PersonsList.UpdatePerson.Execute().Subscribe();
    }

    public void PersonsDataGrid_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        foreach (var itemToSelect in e.AddedItems.OfType<SelectablePersonViewModel>())
            itemToSelect.IsSelected = true;

        foreach (var itemToUnselect in e.RemovedItems.OfType<SelectablePersonViewModel>())
            itemToUnselect.IsSelected = false;
    }
}
