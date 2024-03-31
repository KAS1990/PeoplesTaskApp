namespace PeoplesTaskApp.ViewModels
{
    public abstract class ModelHolderViewModel<TModel>(TModel model) : ViewModelBase
        where TModel : notnull
    {
        protected readonly TModel _model = model;
    }
}
