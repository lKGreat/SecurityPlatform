using AutoMapper;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAtlasApplication(this IServiceCollection services)
    {
        services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
        return services;
    }
}
