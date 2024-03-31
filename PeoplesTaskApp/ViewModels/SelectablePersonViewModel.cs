using PeoplesTaskApp.Models;
using ReactiveUI.Fody.Helpers;

namespace PeoplesTaskApp.ViewModels
{
    public class SelectablePersonViewModel(Person model) : ReadOnlyPersonViewModel(model)
    {
        // В модели это поле не нужно
        [Reactive] public bool IsSelected { get; set; }
    }
}
