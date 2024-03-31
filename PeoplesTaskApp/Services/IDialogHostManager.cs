using PeoplesTaskApp.DataTemplates;
using System.Threading.Tasks;

namespace PeoplesTaskApp.Services
{
    public interface IDialogHostManager
    {
        Task<object?> ShowDialogAsync(string dialogIdentifier, DialogContentInfo contentInfo);
    }
}
