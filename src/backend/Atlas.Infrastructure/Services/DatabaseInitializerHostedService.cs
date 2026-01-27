using Atlas.Application.Abstractions;
using Atlas.Application.Options;
using Atlas.Application.Security;
using Atlas.Core.Tenancy;
using Atlas.Domain.Alert.Entities;
using Atlas.Domain.Assets.Entities;
using Atlas.Domain.Audit.Entities;
using Atlas.Domain.Identity.Entities;
using Atlas.Infrastructure.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlSugar;

namespace Atlas.Infrastructure.Services;

public sealed class DatabaseInitializerHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly BootstrapAdminOptions _bootstrapOptions;
    private readonly PasswordPolicyOptions _passwordPolicy;
    private readonly DatabaseEncryptionOptions _encryptionOptions;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<DatabaseInitializerHostedService> _logger;

    public DatabaseInitializerHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<BootstrapAdminOptions> bootstrapOptions,
        IOptions<PasswordPolicyOptions> passwordPolicy,
        IOptions<DatabaseEncryptionOptions> encryptionOptions,
        IHostEnvironment environment,
        ILogger<DatabaseInitializerHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _bootstrapOptions = bootstrapOptions.Value;
        _passwordPolicy = passwordPolicy.Value;
        _encryptionOptions = encryptionOptions.Value;
        _environment = environment;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_encryptionOptions.Enabled && string.IsNullOrWhiteSpace(_encryptionOptions.Key))
        {
            throw new InvalidOperationException("已启用数据库加密但未配置密钥。");
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        db.CodeFirst.InitTables<UserAccount, AuditRecord, Asset, AlertRecord>();

        if (!_bootstrapOptions.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_bootstrapOptions.Password))
        {
            if (_environment.IsDevelopment())
            {
                _logger.LogWarning("未配置BootstrapAdmin密码，已跳过创建默认管理员。");
                return;
            }

            throw new InvalidOperationException("生产环境必须配置BootstrapAdmin密码。");
        }

        if (!PasswordPolicy.IsCompliant(_bootstrapOptions.Password, _passwordPolicy, out var message))
        {
            throw new InvalidOperationException($"BootstrapAdmin密码不符合策略：{message}");
        }

        if (!Guid.TryParse(_bootstrapOptions.TenantId, out var tenantGuid))
        {
            throw new InvalidOperationException("BootstrapAdmin TenantId格式错误。");
        }

        var tenantId = new TenantId(tenantGuid);
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserAccountRepository>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var existing = await userRepository.FindByUsernameAsync(tenantId, _bootstrapOptions.Username, cancellationToken);
        if (existing is not null)
        {
            return;
        }

        var hashed = passwordHasher.HashPassword(_bootstrapOptions.Password);
        var account = new UserAccount(tenantId, _bootstrapOptions.Username, hashed, _bootstrapOptions.Roles);
        await userRepository.AddAsync(account, cancellationToken);
        _logger.LogInformation("已创建BootstrapAdmin账号：{Username}", _bootstrapOptions.Username);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
