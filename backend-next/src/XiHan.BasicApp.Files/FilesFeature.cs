// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Files;
using XiHan.BasicApp.Contracts.Pages;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Contracts.Workbench;
using XiHan.BasicApp.Files.Domain.Services;
using XiHan.BasicApp.Files.Infrastructure;
using XiHan.BasicApp.Files.Jobs;
using XiHan.BasicApp.Files.Queries;
using XiHan.BasicApp.Files.Seeds;
using XiHan.BasicApp.Runtime;
using XiHan.BasicApp.Runtime.Seeding;
using XiHan.Framework.Kernel;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Localization.Abstractions;
using XiHan.Framework.ObjectStorage.Abstractions;
using XiHan.Framework.ObjectStorage.Abstractions.Stores;
using XiHan.Framework.ObjectStorage.FileSystem;
using XiHan.Framework.ObjectStorage.S3;
using XiHan.Framework.Scheduling.Abstractions;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Persistence.Abstractions;
using XiHan.Framework.Primitives;
using XiHan.Framework.Primitives.Manifests;
using XiHan.Framework.Security.Abstractions;

namespace XiHan.BasicApp.Files;

/// <summary>
/// 文件能力包：存储配置、文件与副本、导出中心与导入历史。
/// </summary>
/// <remarks>
/// Tier 2：存储配置（平台库实体，随所属侧）、文件与副本（租户库实体，平台一侧以 <c>platform</c> 为租户）。
/// 导出中心与导入历史也在这里：导出由后台循环（<see cref="ExportRunner"/>）以发起人的身份执行。
/// 大文件走分片上传（<see cref="FileUploadService"/>，各分片经签名地址直传存储），过了时限的由作业 <see cref="FileUploadExpiryJob"/> 记为过期。
/// 操作只管元数据；上传（<c>POST /api/files/upload</c>）与按身份读（<c>GET /api/files/{id}/content</c>）是宿主映射的普通端点，调用这里的 <see cref="FileService"/>。
/// 对象存储用框架的命名存储实例（<see cref="StorageInstances"/>）；签名地址要宿主装上代收发的签名端点（<c>HostedBlobUrlFeature</c> 与 <c>MapXiHanBlobs()</c>），本地存储的地址由它收发。
/// </remarks>
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
            new CapabilityRequirement(ObjectStorageCapabilities.Presign, ObjectStorageCapabilities.Version),
            new CapabilityRequirement(SchedulingCapabilities.Jobs, SchedulingCapabilities.Version),
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
        // 存储配置的实例由框架注册表按定义建出：本地与 S3 兼容存储两个 Provider
        context.Services.AddBlobStoreProvider<FileSystemBlobStoreProvider>(context.Configuration);
        context.Services.AddBlobStoreProvider<S3BlobStoreProvider>(context.Configuration);
        context.Services.AddSingleton<StorageInstances>();
        context.Services.AddScoped<StorageConfigService>();
        context.Services.AddScoped<FileService>();
        context.Services.AddScoped<FileUploadService>();
        context.Services.AddJob<FileUploadExpiryJob>(FileUploadExpiryJob.Name, FileUploadExpiryJob.Schedule);
        context.Services.AddScoped<FileQueries>();
        context.Services.AddScoped<IWorkbenchWidgetProvider, FilesWidgets>();
        context.Services.AddScoped<IStorageUsage, FileStorageUsage>();
        context.Services.AddScoped<IAvatarFiles, AvatarFiles>();
        context.Services.AddScoped<StorageConfigQueries>();
        context.Services.AddSingleton<ExportJobs>();
        context.Services.AddSingleton<ExportRunner>();
        context.Services.AddScoped<ExportTaskService>();
        context.Services.AddScoped<ExportTaskQueries>();
        context.Services.AddSeeder<StorageConfigSeeder>();
        context.Services.AddPermissionCatalog("files", FilesPermissions.All);
        // 字段安全：实体由响应类型生成，读、建、改的规则由 Access 的中间件统一套用
        FilesFieldSecurity.Register(context.Services);
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
        // 本地存储的实例先登记上：签名地址跨进程重启仍有效，宿主的签名端点按实例名取
        var local = await services.GetRequiredService<StorageInstances>().LocalAsync(cancellationToken).ConfigureAwait(false);
        if (local.IsFailure)
        {
            throw new InvalidOperationException($"The local file store cannot be created ({local.Error!.Code}).");
        }

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
