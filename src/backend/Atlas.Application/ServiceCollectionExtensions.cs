using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAtlasApplication(this IServiceCollection services)
    {
        return services;
    }
}