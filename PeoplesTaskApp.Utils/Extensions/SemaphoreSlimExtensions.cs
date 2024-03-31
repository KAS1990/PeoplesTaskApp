namespace PeoplesTaskApp.Utils.Extensions
{
    public static class SemaphoreSlimExtensions
    {
        public static async Task<IDisposable> UseWaitAsync(this SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync();
            return new ReleaseWrapper(semaphore);
        }

        public static IDisposable UseWait(this SemaphoreSlim semaphore)
        {
            semaphore.Wait();
            return new ReleaseWrapper(semaphore);
        }


        private sealed class ReleaseWrapper(SemaphoreSlim semaphore) : IDisposable
        {
            private readonly SemaphoreSlim _semaphore = semaphore;

            private int _isDisposed;

            public void Dispose()
            {
                if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 1)
                    return;

                _semaphore.Release();
            }
        }
    }
}
