using PeoplesTaskApp.Utils.Models;
using System.Reactive;
using System.Reactive.Disposables;

namespace PeoplesTaskApp.Utils.Services.DataSources
{
    public abstract class DataConverterBase<TInputData, TOutputData> : DataSourceBase<TOutputData>
    {
        private readonly IDataService<TInputData> _wrappee;

        public DataConverterBase(IDataService<TInputData> wrappee)
        {
            _wrappee = wrappee;

            _wrappee.LoadingProgress
                .Subscribe(item =>
                {
                    switch (item.Step)
                    {
                        case SaveLoadStepType.NotStarted:
                        case SaveLoadStepType.Start:
                            _loadingProgressSource.OnNext(item);
                            break;

                        case SaveLoadStepType.Progress:
                            _loadingProgressSource.OnNext(DataSaveLoadProgressItem.ConvertToRange(item, 0, 50));
                            break;

                        case SaveLoadStepType.End:  // Skip
                            if (item.HasError)
                                _loadingProgressSource.OnNext(item);
                            break;

                        default:
                            _loadingProgressSource.OnNext(item);
                            break;
                    }
                })
                .DisposeWith(DisposableOnDestroy);

            _wrappee.SavingProgress
                .Subscribe(item =>
                {
                    switch (item.Step)
                    {
                        case SaveLoadStepType.NotStarted:
                            _savingProgressSource.OnNext(item);
                            break;

                        case SaveLoadStepType.Start:    // Skip
                            if (item.HasError)
                                _savingProgressSource.OnNext(item);
                            break;

                        case SaveLoadStepType.Progress:
                        case SaveLoadStepType.End:
                            _savingProgressSource.OnNext(DataSaveLoadProgressItem.ConvertToRange(item, 50, 100));
                            break;

                        default:
                            _savingProgressSource.OnNext(item);
                            break;
                    }
                })
                .DisposeWith(DisposableOnDestroy);
        }

        protected abstract Task<TOutputData> ConvertAsync(TInputData inputData,
            IProgress<DataSaveLoadProgressItem> progress,
            CancellationToken cancellation = default);

        protected abstract Task<TInputData> ConvertBackAsync(TOutputData outputData,
            IProgress<DataSaveLoadProgressItem> progress,
            CancellationToken cancellation = default);

        public override async Task<TOutputData> LoadAsync(CancellationToken cancellation = default)
        {
            var dataFromWrappee = await _wrappee.LoadAsync(cancellation);

            var data = await ConvertAsync(dataFromWrappee, _loadingProgressSource.ToProgress(), cancellation);

            _loadingProgressSource.OnNext(DataSaveLoadProgressItem.GenerateSucseed(0, 100));

            return data;
        }

        public override async Task SaveAsync(TOutputData data, CancellationToken cancellation = default)
        {
            _savingProgressSource.OnNext(DataSaveLoadProgressItem.GenerateStart(0, 100));

            var dataToWrappee = await ConvertBackAsync(data, _savingProgressSource.ToProgress(), cancellation);

            await _wrappee.SaveAsync(dataToWrappee, cancellation);
        }
    }
}
