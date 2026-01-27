using Atlas.Application.Abstractions;
using Atlas.Application.Alert.Abstractions;
using Atlas.Application.Assets.Abstractions;
using Atlas.Application.Assets.Repositories;
using Atlas.Application.Audit.Abstractions;
using Atlas.Core.Abstractions;
using Atlas.Core.Tenancy;
using Atlas.Infrastructure.IdGen;
using Atlas.Infrastructure.Options;
using Atlas.Infrastructure.Repositories;
using Atlas.Infrastructure.Security;
using Atlas.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SqlSugar;

namespace Atlas.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAtlasInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection("Database"));
        services.Configure<DatabaseBackupOptions>(configuration.GetSection("Database:Backup"));
        services.Configure<DatabaseEncryptionOptions>(configuration.GetSection("Database:Encryption"));
        services.Configure<SnowflakeOptions>(configuration.GetSection("Snowflake"));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IIdGenerator, SnowflakeIdGenerator>();
        services.AddScoped<IAuthTokenService, JwtAuthTokenService>();
        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<IUserAccountRepository, UserAccountRepository>();
        services.AddScoped<IAssetRepository, AssetRepository>();
        services.AddScoped<IAssetQueryService, AssetQueryService>();
        services.AddScoped<IAssetCommandService, AssetCommandService>();
        services.AddScoped<IAuditQueryService, AuditQueryService>();
        services.AddScoped<IAuditWriter, AuditWriter>();
        services.AddScoped<IAlertQueryService, AlertQueryService>();
        services.AddHostedService<DatabaseInitializerHostedService>();
        services.AddHostedService<DatabaseBackupHostedService>();

        services.AddScoped<ISqlSugarClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            var tenantProvider = sp.GetRequiredService<ITenantProvider>();
            var tenantId = tenantProvider.GetTenantId();

            var config = new ConnectionConfig
            {
                ConnectionString = options.ConnectionString,
                DbType = DbType.Sqlite,
                IsAutoCloseConnection = true
            };

            var db = new SqlSugarScope(config);
            if (!tenantId.IsEmpty)
            {
                db.QueryFilter.AddTableFilter<Atlas.Core.Abstractions.TenantEntity>(
                    it => it.TenantIdValue == tenantId.Value);
            }

            return db;
        });

        return services;
    }
}
