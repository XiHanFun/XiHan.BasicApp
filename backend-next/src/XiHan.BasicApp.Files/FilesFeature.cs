// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Files;
using XiHan.BasicApp.Contracts.Pages;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Files.Domain.Services;
using XiHan.BasicApp.Files.Infrastructure;
using XiHan.BasicApp.Files.Queries;
using XiHan.BasicApp.Files.Seeds;
using XiHan.BasicApp.Runtime;
using XiHan.BasicApp.Runtime.Seeding;
using XiHan.Framework.Kernel;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Localization.Abstractions;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Persistence.Abstractions;
using XiHan.Framework.Primitives;
using XiHan.Framework.Primitives.Manifests;
using XiHan.Framework.Security.Abstractions;

namespace XiHan.BasicApp.Files;

/// <summary>
/// 文件能力包（Tier 2）：存储配置（平台库实体，随所属侧）、文件与副本（租户库实体，平台一侧以 <c>platform</c> 为租户）。
/// 导出中心与导入历史也在这里：导出由后台循环（<see cref="ExportRunner"/>）以发起人的身份执行。
/// 操作只管元数据；上传（<c>POST /api/files/upload</c>）与下载（<c>GET /api/files/{id}/content</c>、签名地址 <c>GET /api/files/content/{token}</c>）是宿主映射的普通端点，调用这里的 <see cref="FileService"/>。
/// </summary>
public sealed class FilesFeature : IHostedFeature, IDisposable
{
    private ExportRunner? _runner;
    private CancellationTokenSource? _stopping;
    private Task? _loop;

    /// <inheritdoc />
    public Manifest Manifest { get; } = new()
    {
        Id = AppInfo.ManifestIdPrefix + "files",
        Package = "XiHan.BasicApp.Files",
        Version = AppInfo.Version,
        Primitives = 1,
        Stability = Stability.Preview,
        Kind = ManifestKind.Capability,
        Provides = [new CapabilityDeclaration(FilesCapabilities.Files, FilesCapabilities.Version)],
        Requires =
        [
            new CapabilityRequirement(KernelCapabilities.Hosting, KernelCapabilities.Version),
            new CapabilityRequirement(PersistenceCapabilities.Relational, PersistenceCapabilities.Version),
            new CapabilityRequirement(SecurityCapabilities.Authorization, SecurityCapabilities.Version),
            new CapabilityRequirement(MultiTenancyCapabilities.Context, MultiTenancyCapabilities.Version),
            new CapabilityRequirement(RuntimeCapabilities.Runtime, RuntimeCapabilities.Version),
        ],
        Contributes = new Contributions
        {
            Permissions = FilesPermissions.All.Select(static p => p.Code).ToList(),
        },
        Entry = typeof(FilesFeature).FullName,
    };

    /// <inheritdoc />
    public void Configure(FeatureContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Services.AddMessageCatalog(FilesMessages.Catalog);
        context.Services.AddGeneratedRepositories();
        context.Services.AddSingleton(static services => FilesOptions.FromConfiguration(services.GetRequiredService<IConfiguration>(), services.GetService<IHostEnvironment>()));
        context.Services.AddSingleton<BlobStores>();
        context.Services.AddSingleton<FileLinks>();
        context.Services.AddScoped<StorageConfigService>();
        context.Services.AddScoped<FileService>();
        context.Services.AddScoped<FileQueries>();
        context.Services.AddScoped<IStorageUsage, FileStorageUsage>();
        context.Services.AddScoped<StorageConfigQueries>();
        context.Services.AddSingleton<ExportJobs>();
        context.Services.AddSingleton<ExportRunner>();
        context.Services.AddScoped<ExportTaskService>();
        context.Services.AddScoped<ExportTaskQueries>();
        context.Services.AddSeeder<StorageConfigSeeder>();
        context.Services.AddPermissionCatalog("files", FilesPermissions.All);
        context.Services.AddPageCatalog("files", FilesPages.All);
        foreach (var permission in FilesPermissions.All)
        {
            context.AddPermission(permission.ToDefinition());
        }

        context.AddGeneratedOperations();
    }

    /// <inheritdoc />
    public async Task StartAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(services);
        _runner = services.GetRequiredService<ExportRunner>();
        // 上个进程没执行完的导出：凭据随进程丢了，记为中断
        await _runner.InterruptLeftoversAsync(cancellationToken).ConfigureAwait(false);
        _stopping = new CancellationTokenSource();
        _loop = _runner.RunAsync(_stopping.Token);
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_stopping is null || _loop is null)
        {
            return;
        }

        await _stopping.CancelAsync().ConfigureAwait(false);
        await _loop.ConfigureAwait(false);
        _stopping.Dispose();
        _stopping = null;
        _loop = null;
        _runner = null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _stopping?.Dispose();
        _stopping = null;
    }
}
