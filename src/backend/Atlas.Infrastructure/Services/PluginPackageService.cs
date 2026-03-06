using System.IO.Compression;
using System.Text.Json;
using Atlas.Application.Plugins.Abstractions;
using Atlas.Application.Plugins.Models;
using Atlas.Infrastructure.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Atlas.Infrastructure.Services;

/// <summary>
/// 插件包安装服务：解析 .atpkg（ZIP）包，校验 manifest，复制到插件目录后触发重载。
/// 包结构：
///   manifest.json       -- 元数据
///   lib/                -- DLL 文件（主程序集 + 依赖）
///   assets/             -- 前端资源（可选）
///   config-schema.json  -- 配置 Schema（可选）
/// </summary>
public sealed class PluginPackageService
{
    private readonly IPluginCatalogService _catalogService;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly PluginCatalogOptions _options;
    private readonly ILogger<PluginPackageService> _logger;

    public PluginPackageService(
        IPluginCatalogService catalogService,
        IHostEnvironment hostEnvironment,
        IOptions<PluginCatalogOptions> options,
        ILogger<PluginPackageService> logger)
    {
        _catalogService = catalogService;
        _hostEnvironment = hostEnvironment;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// 从上传的 .atpkg 文件流安装插件。
    /// </summary>
    public async Task<PluginManifest> InstallAsync(
        Stream packageStream,
        CancellationToken cancellationToken)
    {
        var pluginRoot = ResolvePluginRoot();

        // 1. 将包流存到临时文件
        var tempPath = Path.GetTempFileName() + ".zip";
        try
        {
            await using (var fs = File.Create(tempPath))
            {
                await packageStream.CopyToAsync(fs, cancellationToken);
            }

            // 2. 解析并校验 manifest
            var manifest = ReadManifest(tempPath);
            ValidateManifest(manifest);

            // 3. 提取主程序集到插件目录
            ExtractAssembly(tempPath, manifest, pluginRoot);

            // 4. 触发插件目录重载
            await _catalogService.ReloadAsync(cancellationToken);

            _logger.LogInformation(
                "Plugin {Code} v{Version} installed from package",
                manifest.Code, manifest.Version);

            return manifest;
        }
        finally
        {
            try { File.Delete(tempPath); } catch { /* ignore */ }
        }
    }

    private string ResolvePluginRoot()
    {
        if (Path.IsPathRooted(_options.RootPath))
        {
            return _options.RootPath;
        }

        return Path.Combine(_hostEnvironment.ContentRootPath, _options.RootPath);
    }

    private static PluginManifest ReadManifest(string zipPath)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        var manifestEntry = archive.GetEntry("manifest.json")
            ?? throw new InvalidOperationException("插件包缺少 manifest.json");

        using var stream = manifestEntry.Open();
        var manifest = JsonSerializer.Deserialize<PluginManifest>(stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return manifest ?? throw new InvalidOperationException("manifest.json 解析失败");
    }

    private static void ValidateManifest(PluginManifest manifest)
    {
        if (string.IsNullOrWhiteSpace(manifest.Code))
        {
            throw new InvalidOperationException("插件 manifest.Code 不能为空");
        }

        if (string.IsNullOrWhiteSpace(manifest.Assembly))
        {
            throw new InvalidOperationException("插件 manifest.Assembly 不能为空");
        }

        if (string.IsNullOrWhiteSpace(manifest.Version))
        {
            throw new InvalidOperationException("插件 manifest.Version 不能为空");
        }
    }

    private static void ExtractAssembly(string zipPath, PluginManifest manifest, string pluginRootPath)
    {
        Directory.CreateDirectory(pluginRootPath);

        using var archive = ZipFile.OpenRead(zipPath);

        // 提取 lib/ 下的所有 DLL
        foreach (var entry in archive.Entries)
        {
            if (!entry.FullName.StartsWith("lib/", StringComparison.OrdinalIgnoreCase) ||
                !entry.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var fileName = Path.GetFileName(entry.FullName);
            var destPath = Path.Combine(pluginRootPath, fileName);
            entry.ExtractToFile(destPath, overwrite: true);
        }
    }
}

    private static PluginManifest ReadManifest(string zipPath)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        var manifestEntry = archive.GetEntry("manifest.json")
            ?? throw new InvalidOperationException("插件包缺少 manifest.json");

        using var stream = manifestEntry.Open();
        var manifest = JsonSerializer.Deserialize<PluginManifest>(stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return manifest ?? throw new InvalidOperationException("manifest.json 解析失败");
    }

    private static void ValidateManifest(PluginManifest manifest)
    {
        if (string.IsNullOrWhiteSpace(manifest.Code))
        {
            throw new InvalidOperationException("插件 manifest.Code 不能为空");
        }

        if (string.IsNullOrWhiteSpace(manifest.Assembly))
        {
            throw new InvalidOperationException("插件 manifest.Assembly 不能为空");
        }

        if (string.IsNullOrWhiteSpace(manifest.Version))
        {
            throw new InvalidOperationException("插件 manifest.Version 不能为空");
        }
    }

    private static void ExtractAssembly(string zipPath, PluginManifest manifest, string pluginRootPath)
    {
        Directory.CreateDirectory(pluginRootPath);

        using var archive = ZipFile.OpenRead(zipPath);

        // 提取 lib/ 下的所有 DLL
        foreach (var entry in archive.Entries)
        {
            if (!entry.FullName.StartsWith("lib/", StringComparison.OrdinalIgnoreCase) ||
                !entry.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var fileName = Path.GetFileName(entry.FullName);
            var destPath = Path.Combine(pluginRootPath, fileName);
            entry.ExtractToFile(destPath, overwrite: true);
        }
    }
}
