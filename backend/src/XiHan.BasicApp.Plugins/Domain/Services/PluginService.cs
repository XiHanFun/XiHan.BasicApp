// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Files.Domain.Services;
using XiHan.BasicApp.Plugins.Domain.Entities;
using XiHan.BasicApp.Plugins.Domain.Packages;
using XiHan.BasicApp.Plugins.Operations.Plugins;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Capabilities;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;
using XiHan.Framework.Primitives.Manifests;

namespace XiHan.BasicApp.Plugins.Domain.Services;

/// <summary>
/// 插件的安装、升级、回退、授权与卸载
/// </summary>
/// <remarks>
/// 插件包是平台一侧在 Files 里上传的文件：读到临时文件上再按 zip 解析（包可能上百 MB，不整个读进内存），核对交给 <see cref="PluginInspector"/>
/// 新装：清单 ID 与短名都不能与已安装的重复。升级：同一清单 ID 与短名、比当前更新且没上传过的版本。回退：换回一个已上传过的版本，先按现在的配置判一次信任
/// 授予要含核对给出的全部必须项，另外只能有可选项；换版本时按新版本申请的授予整组替换
/// </remarks>
/// <param name="files">文件服务</param>
/// <param name="inspector">插件包核对</param>
/// <param name="trust">信任判定</param>
/// <param name="options">插件模块选项</param>
/// <param name="plugins">插件仓储</param>
/// <param name="versions">版本仓储</param>
/// <param name="grants">授予仓储</param>
/// <param name="ids">标识生成器</param>
public sealed class PluginService(FileService files, PluginInspector inspector, PluginTrust trust, PluginsOptions options, IRepository<SysPlugin, Guid> plugins, IRepository<SysPluginVersion, Guid> versions, IRepository<SysPluginGrant, Guid> grants, IIdGenerator<Guid> ids)
{
    /// <summary>
    /// 复制包内容的缓冲区大小
    /// </summary>
    private const int BufferSize = 81920;

    /// <summary>
    /// 插件不存在的错误
    /// </summary>
    /// <param name="id">插件 ID</param>
    public static Error NotFound(Guid id) => Error.NotFound(PluginsErrors.PluginNotFound, PluginsMessages.PluginNotFound, MessageArguments.Of(("id", id)));

    /// <summary>
    /// 读并核对一个已上传的插件包
    /// </summary>
    /// <remarks>
    /// 文件不在平台一侧即 Files 的 <c>FILES_NOTFOUND_*</c>；包或清单不合规则见 <see cref="PluginPackage.ReadAsync"/> 与 <see cref="PluginInspector.Inspect"/>
    /// 新装（不给插件 ID）：清单 ID 或短名已经安装过 <c>PLUGINS_CONFLICT_101</c>
    /// 升级（给插件 ID）：插件不存在 <c>PLUGINS_NOTFOUND_101</c>，包是别的插件 <c>PLUGINS_VALIDATION_110</c>，版本上传过 <c>PLUGINS_CONFLICT_104</c>、不比当前新 <c>PLUGINS_CONFLICT_103</c>；当前版本登记的操作与权限码不算冲突，结果带上与当前版本的对照
    /// </remarks>
    /// <param name="fileId">文件 ID</param>
    /// <param name="pluginId">要升级的插件；新装为空</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<Result<PluginInspection>> InspectAsync(Guid fileId, Guid? pluginId, CancellationToken cancellationToken)
    {
        SysPlugin? target = null;
        IReadOnlyList<SysPluginVersion> uploaded = [];
        if (pluginId is { } id)
        {
            if (!(await plugins.FindAsync(id, cancellationToken).ConfigureAwait(false)).TryGetValue(out target))
            {
                return NotFound(id);
            }

            uploaded = await VersionsOfAsync(id, cancellationToken).ConfigureAwait(false);
        }

        var current = target is null ? null : uploaded.First(v => v.Id == target.CurrentVersionId);
        var currentManifest = current is null ? null : ManifestReader.Read(current.ManifestJson).ValueOrThrow();
        var owned = currentManifest is null
            ? new HashSet<string>(StringComparer.Ordinal)
            : currentManifest.Contributes.Operations.Concat(currentManifest.Contributes.Permissions).ToHashSet(StringComparer.Ordinal);

        var found = await files.FindAsync(TenantIds.Platform, fileId, cancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var file))
        {
            return found.Error!;
        }

        if (file.Size > options.MaxPackageBytes)
        {
            return Error.Validation(PluginsErrors.PackageInvalid, PluginsMessages.PackageTooLarge, MessageArguments.Of(("max", options.MaxPackageSizeMegabytes)));
        }

        var opened = await files.OpenAsync(file, null, cancellationToken).ConfigureAwait(false);
        if (!opened.TryGetValue(out var content))
        {
            return opened.Error!;
        }

        // zip 要能定位：落到关闭即删的临时文件上，不整个读进内存
        var temporary = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, BufferSize, FileOptions.DeleteOnClose | FileOptions.Asynchronous);
        await using (temporary.ConfigureAwait(false))
        {
            await using (content.ConfigureAwait(false))
            {
                await content.Content.CopyToAsync(temporary, BufferSize, cancellationToken).ConfigureAwait(false);
            }

            var read = await PluginPackage.ReadAsync(temporary, options.MaxPackageBytes, cancellationToken).ConfigureAwait(false);
            if (!read.TryGetValue(out var package))
            {
                return read.Error!;
            }

            var inspected = inspector.Inspect(package, owned);
            if (!inspected.TryGetValue(out var inspection))
            {
                return inspected.Error!;
            }

            if (target is not null)
            {
                var approved = await GrantsOfAsync(target.Id, cancellationToken).ConfigureAwait(false);
                return UpgradeOf(target, current!, currentManifest!, uploaded, [.. approved.Select(static g => g.GrantValue).Order(StringComparer.Ordinal)], inspection);
            }

            var manifestId = package.Manifest.Id;
            var name = package.Descriptor.Name;
            var taken = await plugins.ListAsync(EntityQuery.All<SysPlugin>().Where(new Specification<SysPlugin>(p => p.ManifestId == manifestId || p.Name == name)).Page(0, 1), cancellationToken).ConfigureAwait(false);
            return taken.Count > 0
                ? Error.Conflict(PluginsErrors.AlreadyInstalled, PluginsMessages.AlreadyInstalled, MessageArguments.Of(("id", taken[0].ManifestId == manifestId ? manifestId : name)))
                : inspection;
        }
    }

    /// <summary>
    /// 安装一个已上传的插件包
    /// </summary>
    /// <remarks>
    /// 核对同 <see cref="InspectAsync"/>，授予不全 <c>PLUGINS_DENIED_101</c>、有不认得的 <c>PLUGINS_VALIDATION_106</c>；装好是停用状态。并发安装同一插件由清单 ID 与短名的唯一索引兜住
    /// </remarks>
    /// <param name="request">安装请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<Result<SysPlugin>> InstallAsync(InstallPluginRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var inspected = await InspectAsync(request.FileId, null, cancellationToken).ConfigureAwait(false);
        if (!inspected.TryGetValue(out var inspection))
        {
            return inspected.Error!;
        }

        var approved = NormalizeGrants(request.Grants, inspection.RequiredGrants, inspection.OptionalGrants);
        if (!approved.TryGetValue(out var granted))
        {
            return approved.Error!;
        }

        var package = inspection.Package;
        var plugin = new SysPlugin
        {
            Id = ids.NewId(),
            ManifestId = package.Manifest.Id,
            Name = package.Descriptor.Name,
            IsEnabled = false,
            Remark = request.Remark,
        };
        var version = NewVersion(plugin.Id, request.FileId, inspection);
        Switch(plugin, version, package.Manifest, package.Descriptor);
        var addedPlugin = await plugins.AddAsync(plugin, cancellationToken).ConfigureAwait(false);
        if (addedPlugin.IsFailure)
        {
            return addedPlugin.Error!;
        }

        var addedVersion = await versions.AddAsync(version, cancellationToken).ConfigureAwait(false);
        if (addedVersion.IsFailure)
        {
            return addedVersion.Error!;
        }

        var written = await ReplaceGrantsAsync(plugin.Id, granted, cancellationToken).ConfigureAwait(false);
        return written.IsFailure ? written.Error! : plugin;
    }

    /// <summary>
    /// 把插件升级到一个已上传的新版本
    /// </summary>
    /// <remarks>
    /// 核对同 <see cref="InspectAsync"/> 的升级；授予按新版本申请的整组替换，规则同安装；交回的并发戳过期由框架仓储报。旧版本留着，可以回退
    /// </remarks>
    /// <param name="plugin">插件</param>
    /// <param name="request">升级请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<Result> UpgradeAsync(SysPlugin plugin, UpgradePluginRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(request);
        var inspected = await InspectAsync(request.FileId, plugin.Id, cancellationToken).ConfigureAwait(false);
        if (!inspected.TryGetValue(out var inspection))
        {
            return inspected.Error!;
        }

        var approved = NormalizeGrants(request.Grants, inspection.RequiredGrants, inspection.OptionalGrants);
        if (!approved.TryGetValue(out var granted))
        {
            return approved.Error!;
        }

        var version = NewVersion(plugin.Id, request.FileId, inspection);
        plugin.ConcurrencyStamp = request.ConcurrencyStamp;
        Switch(plugin, version, inspection.Package.Manifest, inspection.Package.Descriptor);
        var updated = await plugins.UpdateAsync(plugin, cancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated;
        }

        var added = await versions.AddAsync(version, cancellationToken).ConfigureAwait(false);
        return added.IsFailure ? added : await ReplaceGrantsAsync(plugin.Id, granted, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 把插件换回一个已上传过的版本
    /// </summary>
    /// <remarks>
    /// 版本不是这个插件的 <c>PLUGINS_NOTFOUND_102</c>，已经是当前版本 <c>PLUGINS_CONFLICT_105</c>；按现在的配置不受信任 <c>PLUGINS_DENIED_102</c> / <c>PLUGINS_DENIED_103</c>
    /// 授予按那个版本申请的整组替换，规则同安装；交回的并发戳过期由框架仓储报
    /// </remarks>
    /// <param name="plugin">插件</param>
    /// <param name="request">回退请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<Result> RollbackAsync(SysPlugin plugin, RollbackPluginRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(request);
        if (!(await versions.FindAsync(request.VersionId, cancellationToken).ConfigureAwait(false)).TryGetValue(out var version) || version.PluginId != plugin.Id)
        {
            return Error.NotFound(PluginsErrors.VersionNotFound, PluginsMessages.VersionNotFound, MessageArguments.Of(("id", request.VersionId)));
        }

        if (version.Id == plugin.CurrentVersionId)
        {
            return Error.Conflict(PluginsErrors.VersionCurrent, PluginsMessages.VersionCurrent, MessageArguments.Of(("version", version.Version)));
        }

        var manifest = ManifestReader.Read(version.ManifestJson).ValueOrThrow();
        var descriptor = DescriptorOf(version);
        var trusted = trust.Recheck(descriptor.Publisher, new PluginSigner(version.SignatureStatus, version.SignerKey));
        if (trusted.IsFailure)
        {
            return trusted;
        }

        var approved = NormalizeGrants(request.Grants, PluginInspector.RequiredGrantsOf(manifest, descriptor), [PluginInspector.DataScopeAll]);
        if (!approved.TryGetValue(out var granted))
        {
            return approved.Error!;
        }

        plugin.ConcurrencyStamp = request.ConcurrencyStamp;
        Switch(plugin, version, manifest, descriptor);
        var updated = await plugins.UpdateAsync(plugin, cancellationToken).ConfigureAwait(false);
        return updated.IsFailure ? updated : await ReplaceGrantsAsync(plugin.Id, granted, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 升级包与当前版本的对照
    /// </summary>
    /// <remarks>
    /// 不是同一插件或版本不对即失败
    /// </remarks>
    private static Result<PluginInspection> UpgradeOf(SysPlugin plugin, SysPluginVersion current, Manifest currentManifest, IReadOnlyList<SysPluginVersion> uploaded, IReadOnlyList<string> approved, PluginInspection inspection)
    {
        var package = inspection.Package;
        var version = package.Manifest.Version;
        if (!string.Equals(package.Manifest.Id, plugin.ManifestId, StringComparison.Ordinal) || !string.Equals(package.Descriptor.Name, plugin.Name, StringComparison.Ordinal))
        {
            return Error.Validation(PluginsErrors.UpgradeMismatch, PluginsMessages.UpgradeOtherPlugin, MessageArguments.Of(("id", package.Manifest.Id), ("name", package.Descriptor.Name), ("expected", plugin.ManifestId)));
        }

        if (uploaded.Any(v => string.Equals(v.Version, version, StringComparison.Ordinal)))
        {
            return Error.Conflict(PluginsErrors.VersionUploaded, PluginsMessages.VersionUploaded, MessageArguments.Of(("version", version)));
        }

        if (PluginVersions.Compare(version, plugin.Version) <= 0)
        {
            return Error.Conflict(PluginsErrors.VersionNotNewer, PluginsMessages.VersionNotNewer, MessageArguments.Of(("version", version), ("current", plugin.Version)));
        }

        var currentPages = DescriptorOf(current).Pages.Select(static p => p.Code).ToList();
        var nextPages = package.Descriptor.Pages.Select(static p => p.Code).ToList();
        return inspection with
        {
            Upgrade = new PluginUpgrade(
                plugin.Id,
                plugin.Version,
                Added(package.Manifest.Contributes.Operations, currentManifest.Contributes.Operations),
                Added(currentManifest.Contributes.Operations, package.Manifest.Contributes.Operations),
                Added(package.Manifest.Contributes.Permissions, currentManifest.Contributes.Permissions),
                Added(currentManifest.Contributes.Permissions, package.Manifest.Contributes.Permissions),
                Added(nextPages, currentPages),
                Added(currentPages, nextPages),
                approved),
        };
    }

    /// <summary>
    /// 在 <paramref name="next"/> 里、不在 <paramref name="previous"/> 里的，按序数排序
    /// </summary>
    private static IReadOnlyList<string> Added(IEnumerable<string> next, IEnumerable<string> previous)
        => [.. next.Except(previous, StringComparer.Ordinal).Order(StringComparer.Ordinal)];

    /// <summary>
    /// 核对通过的包记成插件的一个版本
    /// </summary>
    private SysPluginVersion NewVersion(Guid pluginId, Guid fileId, PluginInspection inspection) => new()
    {
        Id = ids.NewId(),
        PluginId = pluginId,
        Version = inspection.Package.Manifest.Version,
        FileId = fileId,
        Sha256 = inspection.Package.Sha256,
        Size = inspection.Package.Size,
        ManifestJson = inspection.Package.ManifestJson,
        DescriptorJson = inspection.Package.DescriptorJson,
        SignatureStatus = inspection.Signer.Status,
        SignerKey = inspection.Signer.KeyId,
    };

    /// <summary>
    /// 插件的当前版本换成另一个
    /// </summary>
    /// <remarks>
    /// 列表要的版本号、发布者与隔离级别从它抄过来
    /// </remarks>
    private static void Switch(SysPlugin plugin, SysPluginVersion version, Manifest manifest, PluginDescriptor descriptor)
    {
        plugin.CurrentVersionId = version.Id;
        plugin.Version = version.Version;
        plugin.Publisher = descriptor.Publisher;
        plugin.Isolation = manifest.Isolation!.Value;
    }

    /// <summary>
    /// 版本的旁挂清单
    /// </summary>
    /// <remarks>
    /// 上传时核对过，读不出说明库里的数据被改坏了，按缺陷抛出
    /// </remarks>
    private static PluginDescriptor DescriptorOf(SysPluginVersion version)
        => System.Text.Json.JsonSerializer.Deserialize(version.DescriptorJson, PluginsJsonContext.Default.PluginDescriptor)
            ?? throw new InvalidOperationException($"The descriptor of plugin version '{version.Id}' is empty.");

    /// <summary>
    /// 核对批准的授予
    /// </summary>
    /// <remarks>
    /// 必须项缺一项即 <c>PLUGINS_DENIED_101</c>（缺的项列在 <c>grants</c> 里），既不是必须项也不是可选项的 <c>PLUGINS_VALIDATION_106</c>；重复的合并，结果按序数排序
    /// </remarks>
    /// <param name="approved">批准的授予</param>
    /// <param name="required">必须项</param>
    /// <param name="optional">可选项</param>
    public static Result<IReadOnlyList<string>> NormalizeGrants(IReadOnlyList<string> approved, IReadOnlyList<string> required, IReadOnlyList<string> optional)
    {
        ArgumentNullException.ThrowIfNull(approved);
        ArgumentNullException.ThrowIfNull(required);
        ArgumentNullException.ThrowIfNull(optional);
        var set = approved.Select(static g => g.Trim()).ToHashSet(StringComparer.Ordinal);
        if (set.FirstOrDefault(g => !required.Contains(g, StringComparer.Ordinal) && !optional.Contains(g, StringComparer.Ordinal)) is { } unknown)
        {
            return Error.Validation(PluginsErrors.GrantInvalid, PluginsMessages.GrantInvalid, MessageArguments.Of(("grant", unknown)));
        }

        var missing = required.Where(g => !set.Contains(g)).ToList();
        return missing.Count > 0
            ? Error.Denied(PluginsErrors.GrantsMissing, PluginsMessages.GrantsMissing, MessageArguments.Of(("grants", string.Join(", ", missing))))
            : set.Order(StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// 整组替换一个插件的授予
    /// </summary>
    /// <param name="pluginId">插件 ID</param>
    /// <param name="granted">核对过的授予</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<Result> ReplaceGrantsAsync(Guid pluginId, IReadOnlyList<string> granted, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(granted);
        var current = await GrantsOfAsync(pluginId, cancellationToken).ConfigureAwait(false);
        foreach (var result in await grants.RemoveRangeAsync(current, cancellationToken).ConfigureAwait(false))
        {
            if (result.IsFailure)
            {
                return result;
            }
        }

        foreach (var result in await grants.AddRangeAsync([.. granted.Select(g => new SysPluginGrant { Id = ids.NewId(), PluginId = pluginId, GrantValue = g })], cancellationToken).ConfigureAwait(false))
        {
            if (result.IsFailure)
            {
                return result;
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// 一个插件已批准的授予
    /// </summary>
    /// <param name="pluginId">插件 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task<IReadOnlyList<SysPluginGrant>> GrantsOfAsync(Guid pluginId, CancellationToken cancellationToken)
        => grants.ListAsync(EntityQuery.All<SysPluginGrant>().Where(new Specification<SysPluginGrant>(g => g.PluginId == pluginId)), cancellationToken);

    /// <summary>
    /// 一个插件的全部版本，新的在前
    /// </summary>
    /// <param name="pluginId">插件 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<IReadOnlyList<SysPluginVersion>> VersionsOfAsync(Guid pluginId, CancellationToken cancellationToken)
        => [.. (await versions.ListAsync(EntityQuery.All<SysPluginVersion>().Where(new Specification<SysPluginVersion>(v => v.PluginId == pluginId)), cancellationToken).ConfigureAwait(false)).OrderByDescending(static v => v.CreatedAt)];

    /// <summary>
    /// 插件全部版本贡献过的权限码
    /// </summary>
    /// <remarks>
    /// 卸载要清掉对它们的引用：升级换掉的旧版本贡献过的码也算，授予可能还留着
    /// </remarks>
    /// <param name="pluginId">插件 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<IReadOnlyList<string>> PermissionsOfAsync(Guid pluginId, CancellationToken cancellationToken)
        => [.. (await VersionsOfAsync(pluginId, cancellationToken).ConfigureAwait(false))
            .SelectMany(static v => (System.Text.Json.JsonSerializer.Deserialize(v.DescriptorJson, PluginsJsonContext.Default.PluginDescriptor)?.Permissions ?? []).Select(static p => p.Code))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)];

    /// <summary>
    /// 插件的详情
    /// </summary>
    /// <remarks>
    /// 贡献与申请的授予按当前版本的清单原文解释；当前版本的清单读不出说明库里的数据被改坏了，按缺陷抛出
    /// </remarks>
    /// <param name="plugin">插件</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<PluginDetailResponse> DetailAsync(SysPlugin plugin, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        var all = await VersionsOfAsync(plugin.Id, cancellationToken).ConfigureAwait(false);
        var current = all.First(v => v.Id == plugin.CurrentVersionId);
        var manifest = ManifestReader.Read(current.ManifestJson).ValueOrThrow();
        var descriptor = System.Text.Json.JsonSerializer.Deserialize(current.DescriptorJson, PluginsJsonContext.Default.PluginDescriptor)
            ?? throw new InvalidOperationException($"The descriptor of plugin '{plugin.ManifestId}' version '{current.Version}' is empty.");
        var approved = await GrantsOfAsync(plugin.Id, cancellationToken).ConfigureAwait(false);
        return new PluginDetailResponse(
            PluginResponse.From(plugin),
            PluginContributionsResponse.From(manifest, descriptor),
            PluginInspector.RequiredGrantsOf(manifest, descriptor),
            [PluginInspector.DataScopeAll],
            [.. approved.Select(static g => g.GrantValue).Order(StringComparer.Ordinal)],
            [.. all.Select(v => new PluginVersionResponse(v.Id, v.Version, v.FileId, v.Sha256, v.Size, new PluginSignatureResponse(v.SignatureStatus, v.SignerKey), PluginInspector.RequiredGrantsOf(ManifestReader.Read(v.ManifestJson).ValueOrThrow(), DescriptorOf(v)), v.Id == plugin.CurrentVersionId, v.CreatedAt))]);
    }

    /// <summary>
    /// 卸载一个插件：插件、全部版本与授予
    /// </summary>
    /// <remarks>
    /// 插件与版本软删除（同一插件可以再装），授予物理删除；包文件留在 Files 里，由 Files 自己管
    /// </remarks>
    /// <param name="plugin">插件</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<Result> UninstallAsync(SysPlugin plugin, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        var cleared = await ReplaceGrantsAsync(plugin.Id, [], cancellationToken).ConfigureAwait(false);
        if (cleared.IsFailure)
        {
            return cleared;
        }

        foreach (var result in await versions.RemoveRangeAsync(await VersionsOfAsync(plugin.Id, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false))
        {
            if (result.IsFailure)
            {
                return result;
            }
        }

        return await plugins.RemoveAsync(plugin, cancellationToken).ConfigureAwait(false);
    }
}
