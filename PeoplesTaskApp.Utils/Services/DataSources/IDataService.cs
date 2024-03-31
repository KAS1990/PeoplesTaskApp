using PeoplesTaskApp.Utils.Models;

namespace PeoplesTaskApp.Utils.Services.DataSources
{
    public interface IDataService<TData> : IDataLoader<TData>, IDataSaver<TData>
    {
    }

    public interface IDataLoader<TData>
    {
        public IObservable<DataSaveLoadProgressItem> LoadingProgress { get; }

        /// <summary>
        /// Загрузка данных из хранилища
        /// В случае ошибки выбрасывает исключение, которое предварительно отправляется в <see cref="SavingProgress"/>
        /// </summary>
        /// <returns>
        /// Загруженные данные и результат
        /// </returns>
        Task<TData> LoadAsync(CancellationToken cancellation = default);
    }

    public interface IDataSaver<TData>
    {
        public IObservable<DataSaveLoadProgressItem> SavingProgress { get; }

        /// <summary>
        /// Сохранение данных в хранилище.
        /// В случае ошибки выбрасывает исключение, которое предварительно отправляется в <see cref="SavingProgress"/>
        /// </summary>
        /// <returns></returns>
        Task SaveAsync(TData data, CancellationToken cancellation = default);
    }
}
