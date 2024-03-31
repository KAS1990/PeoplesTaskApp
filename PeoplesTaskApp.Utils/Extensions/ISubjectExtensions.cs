using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Subjects;

namespace PeoplesTaskApp.Utils.Extensions
{
    public static class ISubjectExtensions
    {
        public static void OnCompleteWith<T>(this ISubject<T> subject, CompositeDisposable disposable) =>
            Disposable.Create(subject.OnCompleted).DisposeWith(disposable);

        public static void SetAndComplete<T>(this AsyncSubject<T> asyncSubject, T value)
        {
            asyncSubject.OnNext(value);
            asyncSubject.OnCompleted();
        }

        public static void SetAndComplete(this AsyncSubject<Unit> asyncSubject) => asyncSubject.SetAndComplete(Unit.Default);

        public static void MyNext(this ISubject<Unit> subject) => subject.OnNext(Unit.Default);
    }
}
