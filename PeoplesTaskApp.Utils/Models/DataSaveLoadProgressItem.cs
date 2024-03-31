namespace PeoplesTaskApp.Utils.Models
{
    public enum SaveLoadStepType
    {
        NotStarted,
        Start,
        Progress,
        End
    }

    public class DataSaveLoadProgressItem
    {
        public SaveLoadStepType Step { get; }

        public double MinValue { get; }
        public double MaxValue { get; }
        public double Value { get; }

        public Exception? Exception { get; }

        public bool HasError => Exception is not null;

        private DataSaveLoadProgressItem(SaveLoadStepType step, double minValue, double maxValue, double value, Exception? exception)
        {
            Step = step;
            MinValue = minValue;
            MaxValue = maxValue;
            Value = value;
            Exception = exception;
        }

        public static DataSaveLoadProgressItem GenerateInitial() => new(SaveLoadStepType.NotStarted, 0, 0, 0, null);

        public static DataSaveLoadProgressItem GenerateStart(int minValue, int maxValue) =>
            new(SaveLoadStepType.Start, minValue, maxValue, minValue, null);

        public static DataSaveLoadProgressItem GenerateSucseed(int minValue, int maxValue) =>
            new(SaveLoadStepType.End, minValue, maxValue, maxValue, null);

        public static DataSaveLoadProgressItem GenerateProgress(int minValue, int maxValue, int value) =>
            new(SaveLoadStepType.Progress, minValue, maxValue, value, null);

        public static DataSaveLoadProgressItem GenerateError(SaveLoadStepType step,
                double minValue,
                double maxValue,
                double value,
                Exception exception) =>
            new(step, minValue, maxValue, value, exception);

        public static DataSaveLoadProgressItem ConvertToRange(DataSaveLoadProgressItem baseItem, double minValue, double maxValue) =>
            new(baseItem.Step,
                minValue,
                maxValue,
                minValue + maxValue * (baseItem.Value - baseItem.MinValue) / (baseItem.MaxValue - baseItem.MinValue),
                baseItem.Exception);
    }
}
