using Newtonsoft.Json;
using PeoplesTaskApp.Utils.Models;
using PeoplesTaskApp.Utils.Services.DataSources;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PeoplesTaskApp.Services.DataSources
{
    public class JsonConverter<TOutputData>(IDataService<string[]> wrappee) : DataConverterBase<string[], TOutputData>(wrappee)
    {
        protected override Task<TOutputData> ConvertAsync(string[] inputData,
            IProgress<DataSaveLoadProgressItem> progress,
            CancellationToken cancellation = default)
        {
            try
            {
                var convertedData = JsonConvert.DeserializeObject<TOutputData>(string.Join(" ", inputData));
                return convertedData is null
                    ? throw new InvalidCastException($"Cannot convert string[] to {typeof(TOutputData)} by {nameof(JsonConverter)} - result null")
                    : Task.FromResult(convertedData);
            }
            catch (Exception ex)
            {
                progress.Report(DataSaveLoadProgressItem.GenerateError(SaveLoadStepType.End, 0, 100, 0, ex));
                throw;
            }
        }

        protected override Task<string[]> ConvertBackAsync(TOutputData outputData,
            IProgress<DataSaveLoadProgressItem> progress,
            CancellationToken cancellation = default)
        {
            try
            {
                return Task.FromResult(new string[] { JsonConvert.SerializeObject(outputData) });
            }
            catch (Exception ex)
            {
                progress.Report(DataSaveLoadProgressItem.GenerateError(SaveLoadStepType.End, 0, 100, 0, ex));
                throw;
            }
        }
    }
}
