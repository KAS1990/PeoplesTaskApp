using PeoplesTaskApp.Models;
using System.Reactive.Disposables;

namespace PeoplesTaskApp.ViewModels;

public class MainViewModel : ViewModelBase
{
    public PersonsListViewModel PersonsList { get; }

    public MainViewModel()
    {
        PersonsList = new PersonsListViewModel(new PersonsList().DisposeWith(DisposableOnDestroy)).DisposeWith(DisposableOnDestroy);
    }
}
