using PeoplesTaskApp.Utils.Extensions;
using PeoplesTaskApp.Utils.Services.DataSources;
using Splat;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace PeoplesTaskApp.Desktop
{
    internal sealed class LogImpl : ILogger
    {
        private static readonly TimeSpan MAX_SAVES_DELAY = TimeSpan.FromSeconds(5);
        private const int MAX_LOG_ITEMS_TO_SAVE = 50;

        private readonly IDataSaver<string[]> _dataSaver;

        /// <summary>
        /// Allow to save all items to log before app close
        /// </summary>
        private static readonly AsyncSubject<Unit> _logMessagesFinisher = new();
        private readonly Subject<string> _logMessagesSource = new();

        public LogImpl(IDataSaver<string[]> dataSaver)
        {
            _dataSaver = dataSaver;

            // All GroupBy operations must be performed in the same thread!!!
            // See: https://github.com/dotnet/reactive/issues/839
            var scheduler
                = new EventLoopScheduler(func =>
                    new Thread(func)
                    {
                        Priority = ThreadPriority.Lowest,
                        Name = "Logger"
                    });

            _logMessagesSource
                .DistinctUntilChanged()
                .ObserveOn(scheduler)
                .GroupByUntil(x => 1,
                    x => Observable.Merge(Observable.Timer(MAX_SAVES_DELAY, scheduler),
                            x.Skip(MAX_LOG_ITEMS_TO_SAVE - 1).Select(_ => 0L),
                            Observable.Defer(() => _logMessagesFinisher).Select(_ => 0L).ObserveOn(scheduler)))
                .SelectMany(x => x.ToArray())
                .Where(messages => messages.Length != 0)
#if DEBUG || DEBUG__NET_NATIVE
                .Do(messages => Debug.WriteLine($"Log items: {messages.Length}"))
#endif
                .Subscribe(SaveMessageToFile, () => scheduler.Dispose());

            AppendTextIfNeeded("================ START APP" + Environment.NewLine, LogLevel.Info);
            AppendTextIfNeeded($"Date: {DateTime.UtcNow.ToLongDateString()}", LogLevel.Info);
            AppendTextIfNeeded($"OSVersion: {Environment.OSVersion.VersionString}", LogLevel.Info);
            AppendTextIfNeeded($"Is64BitOperatingSystem: {Environment.Is64BitOperatingSystem}", LogLevel.Info);
            AppendTextIfNeeded($"Is64BitProcess: {Environment.Is64BitProcess}", LogLevel.Info);
            AppendTextIfNeeded($"Version: {Environment.Version}", LogLevel.Info);
        }

        ~LogImpl()
        {
            LastItemGenerated();
        }

        public void LastItemGenerated()
        {
            _logMessagesFinisher.SetAndComplete();
            _logMessagesSource.OnCompleted();
        }

        public void Write(string message, LogLevel logLevel)
        {
            try
            {
                if (CheckLogMessage(message, logLevel))
                    AppendTextIfNeeded(message.EndsWith(Environment.NewLine) ? message : $"{message}{Environment.NewLine}", logLevel);
            }
            catch
            {
                if (Debugger.IsAttached)
                    Debugger.Break();
            }
        }

        //
        // Summary:
        //     Writes a message to the target.
        //
        // Parameters:
        //   exception:
        //     The exception that occured.
        //
        //   message:
        //     The message to write.
        //
        //   logLevel:
        //     The severity level of the log message.
        public void Write(Exception exception, [Localizable(false)] string message, LogLevel logLevel)
        {
            try
            {
                if (!CheckLogMessage(exception.Message + message, logLevel))
                    return;

                var exceptionMessages = exception.GetAllMessagesAsList();
                var exceptionStackTraces = exception.GetAllStackTraceAsList();

                if (message.EndsWith(Environment.NewLine))
                {
                    if (exceptionMessages.EndsWith(Environment.NewLine))
                        AppendTextIfNeeded($"{exceptionMessages}{exceptionStackTraces}{Environment.NewLine}{message}", logLevel);
                    else
                    {
                        AppendTextIfNeeded($"{exceptionMessages}{Environment.NewLine}{exceptionStackTraces}{Environment.NewLine}{message}",
                            logLevel);
                    }
                }
                else if (exceptionMessages.EndsWith(Environment.NewLine))
                    AppendTextIfNeeded($"{exceptionMessages}{exceptionStackTraces}{Environment.NewLine}{message}{Environment.NewLine}", logLevel);
                else
                {
                    AppendTextIfNeeded($"{exceptionMessages}{Environment.NewLine}{exceptionStackTraces}{Environment.NewLine}{message}{Environment.NewLine}",
                        logLevel);
                }
            }
            catch
            {
                if (Debugger.IsAttached)
                    Debugger.Break();
            }
        }

        //
        // Summary:
        //     Writes a messge to the target.
        //
        // Parameters:
        //   message:
        //     The message.
        //
        //   type:
        //     The type.
        //
        //   logLevel:
        //     The log level.
        public void Write([Localizable(false)] string message, [Localizable(false)] Type type, LogLevel logLevel)
        {
            try
            {
                if (!CheckLogMessage(message, logLevel))
                    return;

                if (message.EndsWith(Environment.NewLine))
                    AppendTextIfNeeded($"{type.Name}: {message}", logLevel);
                else
                    AppendTextIfNeeded($"{type.Name}: {message}{Environment.NewLine}", logLevel);
            }
            catch
            {
                if (Debugger.IsAttached)
                    Debugger.Break();
            }
        }

        //
        // Summary:
        //     Writes a messge to the target.
        //
        // Parameters:
        //   exception:
        //     The exception that occured.
        //
        //   message:
        //     The message.
        //
        //   type:
        //     The type.
        //
        //   logLevel:
        //     The log level.
        public void Write(Exception exception, [Localizable(false)] string message, [Localizable(false)] Type type, LogLevel logLevel)
        {
            try
            {
                if (!CheckLogMessage(exception.Message + message, logLevel))
                    return;

                var exceptionMessages = exception.GetAllMessagesAsList();
                var exceptionStackTraces = exception.GetAllStackTraceAsList();

                if (message.EndsWith(Environment.NewLine))
                {
                    if (exceptionMessages.EndsWith(Environment.NewLine))
                        AppendTextIfNeeded($"{exceptionMessages}{type.Name}: {message}{exceptionStackTraces}", logLevel);
                    else
                        AppendTextIfNeeded($"{exceptionMessages}{Environment.NewLine}{type.Name}: {message}{exceptionStackTraces}", logLevel);
                }
                else if (exceptionMessages.EndsWith(Environment.NewLine))
                    AppendTextIfNeeded($"{exceptionMessages}{type.Name}: {message}{Environment.NewLine}{exceptionStackTraces}", logLevel);
                else
                {
                    AppendTextIfNeeded($"{exceptionMessages}{Environment.NewLine}{type.Name}: {message}{Environment.NewLine}{exceptionStackTraces}",
                        logLevel);
                }
            }
            catch
            {
                if (Debugger.IsAttached)
                    Debugger.Break();
            }
        }

        private bool CheckLogMessage(string message, LogLevel logLevel) =>
            (int)logLevel >= (int)Level
            && !message.Contains("POCOObservableForProperty");   // Ignore default unnecessary ReactiveUI log messages

        private void AppendTextIfNeeded(string message, LogLevel logLevel)
        {
            // Leave only one LF char at the end of message
            var lastLFIndex = message.LastIndexOf(Environment.NewLine);
            if (lastLFIndex >= 0 && lastLFIndex < message.Length - 1)
                message = message[..lastLFIndex];

            _logMessagesSource.OnNext($"----[{DateTime.Now:HH:mm:ss.fff}] {message}");
        }

        private void SaveMessageToFile(string[] messages)
        {
            Debug.WriteLine(string.Join(Environment.NewLine, messages));

            try
            {
#pragma warning disable JJ0101 // Method call is missing Await
                _dataSaver.SaveAsync(messages).Wait();
#pragma warning restore JJ0101 // Method call is missing Await
            }
            catch (Exception ex)
            {
#if DEBUG || DEBUG__NET_NATIVE
                ex.WriteToDebugConsole();
#endif
            }
        }

        public LogLevel Level { get; set; }
    }
}
