using Atlas.Application.Platform.Abstractions;
using Atlas.Infrastructure.Services.Platform;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Infrastructure.DependencyInjection;

public static class PlatformServiceRegistration
{
    public static IServiceCollection AddPlatformInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IPlatformQueryService, PlatformQueryService>();
        services.AddScoped<IAppManifestQueryService, AppManifestQueryService>();
        services.AddScoped<IAppManifestCommandService, AppManifestCommandService>();
        services.AddScoped<IAppReleaseCommandService, AppReleaseCommandService>();
        services.AddScoped<IRuntimeRouteQueryService, RuntimeRouteQueryService>();
        return services;
    }
}
