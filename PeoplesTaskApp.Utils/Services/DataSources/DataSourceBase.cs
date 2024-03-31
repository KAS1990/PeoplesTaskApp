using PeoplesTaskApp.Utils.Extensions;
using PeoplesTaskApp.Utils.Models;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace PeoplesTaskApp.Utils.Services.DataSources
{
    public abstract class DataSourceBase<TData> : DisposableBase, IDataService<TData>
    {
        #region SavingProgress

        protected readonly BehaviorSubject<DataSaveLoadProgressItem> _savingProgressSource = new(DataSaveLoadProgressItem.GenerateInitial());
        public IObservable<DataSaveLoadProgressItem> SavingProgress => _savingProgressSource.AsObservable();

        #endregion

        #region LoadingProgress

        protected readonly BehaviorSubject<DataSaveLoadProgressItem> _loadingProgressSource = new(DataSaveLoadProgressItem.GenerateInitial());
        public IObservable<DataSaveLoadProgressItem> LoadingProgress => _savingProgressSource.AsObservable();

        #endregion

        protected DataSourceBase()
        {
            _savingProgressSource.OnCompleteWith(DisposableOnDestroy);
            _loadingProgressSource.OnCompleteWith(DisposableOnDestroy);
        }

        public abstract Task<TData> LoadAsync(CancellationToken cancellation = default);

        public abstract Task SaveAsync(TData data, CancellationToken cancellation = default);
    }
}
