using PeoplesTaskApp.Utils.Extensions;
using PeoplesTaskApp.Utils.Models;
using PeoplesTaskApp.Utils.Services.DataSources;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PeoplesTaskApp.Services.DataSources
{
    public sealed class FileDataSource(string fileName) : DataSourceBase<string[]>
    {
        private readonly string _fileName = fileName;

        /// <summary>
        /// Файл один, поэтому нужна блокировка
        /// </summary>
        private readonly SemaphoreSlim _fileLocker = new(1);

        public override async Task<string[]> LoadAsync(CancellationToken cancellation = default)
        {
            string[] data;

            using (await _fileLocker.UseWaitAsync())
            {
                _loadingProgressSource.OnNext(DataSaveLoadProgressItem.GenerateStart(0, 100));
                try
                {
                    data = await File.ReadAllLinesAsync(_fileName, cancellation);
                    _loadingProgressSource.OnNext(DataSaveLoadProgressItem.GenerateSucseed(0, 100));
                }
                catch (Exception ex)
                {
                    _loadingProgressSource.OnNext(DataSaveLoadProgressItem.GenerateError(SaveLoadStepType.End, 0, 100, 0, ex));
                    throw;
                }
            }

            return data;
        }

        public override async Task SaveAsync(string[] data, CancellationToken cancellation = default)
        {
            using (await _fileLocker.UseWaitAsync())
            {
                _loadingProgressSource.OnNext(DataSaveLoadProgressItem.GenerateStart(0, 100));
                try
                {
                    await File.WriteAllLinesAsync(_fileName, data, cancellation);
                    _loadingProgressSource.OnNext(DataSaveLoadProgressItem.GenerateSucseed(0, 100));
                }
                catch (Exception ex)
                {
                    _loadingProgressSource.OnNext(DataSaveLoadProgressItem.GenerateError(SaveLoadStepType.End, 0, 100, 0, ex));
                    throw;
                }
            }
        }
    }
}
