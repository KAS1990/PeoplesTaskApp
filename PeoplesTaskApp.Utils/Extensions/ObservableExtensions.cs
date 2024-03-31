using System.Reactive;
using System.Reactive.Linq;

namespace PeoplesTaskApp.Utils.Extensions
{
    public static class ObservableExtensions
    {
#pragma warning disable JJ0100 // Method name contains Async prefix
        public static IObservable<TSource> FirstOrNothingAsync<TSource>(this IObservable<TSource> source) => source.Take(1);

        public static IObservable<TSource> FirstOrNothingAsync<TSource>(this IObservable<TSource> source, Func<TSource, bool> predicate) =>
            source.Where(predicate).Take(1);
#pragma warning restore JJ0100 // Method name contains Async prefix

        public static IObservable<Unit> ToUnit<TSource>(this IObservable<TSource> source) => source.Select(_ => Unit.Default);

        public static IObservable<TSource> OnlyNotNull<TSource>(this IObservable<TSource?> source) where TSource : class =>
            source.Where(x => x is not null).Select(x => x!);

        public static IObservable<TSource> OnlyNotNull<TSource>(this IObservable<TSource?> source) where TSource : struct =>
            source.Where(x => x is not null).Select(x => (TSource)x!);
    }
}
