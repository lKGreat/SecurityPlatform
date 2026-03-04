using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Atlas.SecurityPlatform.Tests.Integration.Infrastructure;

/// <summary>
/// 集成测试用 WebApplicationFactory，为每次实例化创建独立临时目录存放 SQLite 数据库，
/// 避免测试间数据污染。Dispose 时清理临时目录，防止 CI 环境下磁盘占用无限增长。
/// </summary>
public sealed class AtlasWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _tempDirectory;

    public AtlasWebApplicationFactory()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "atlas-security-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var dbPath = Path.Combine(_tempDirectory, "atlas.db");

        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ConnectionString"] = $"Data Source={dbPath}",
                ["Database:Backup:Enabled"] = "false",
                ["Security:BootstrapAdmin:Password"] = "Admin@123"
            });
        });

        base.ConfigureWebHost(builder);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_tempDirectory))
        {
            try
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
            catch (IOException)
            {
                // 文件可能仍被占用，忽略清理失败，避免影响测试
            }
        }
    }
}
