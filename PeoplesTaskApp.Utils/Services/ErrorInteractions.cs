using ReactiveUI;
using System.Reactive;

namespace PeoplesTaskApp.Utils.Services
{
    /// <summary>
    /// Содержит Interaction, которые позволяют централизованно обрабатывать любые ошибки, которые возникают в ходе работы программы
    /// </summary>
    public class ErrorInteractions
    {
        public static Interaction<Exception, Unit> UnhandledFatalErrors { get; } = new(RxApp.MainThreadScheduler);
        public static Interaction<Exception, Unit> UnhandledErrors { get; } = new(RxApp.MainThreadScheduler);

        public static Interaction<Exception, Unit> TaskPoolSchedulerErrors { get; } = new(RxApp.MainThreadScheduler);
    }
}
