using Splat;

namespace PeoplesTaskApp.Utils.Extensions
{
    public static class ReadonlyDependencyResolverExtensions
    {
        public static T GetServiceOrThrow<T>(this IReadonlyDependencyResolver resolver, string? contract = null)
            => resolver.GetService<T>(contract) ?? throw new NullReferenceException($"Service {typeof(T).Name} is not registered");
    }
}
