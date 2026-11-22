// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Plugins.Domain.Entities;
using XiHan.BasicApp.Plugins.Domain.Packages;
using XiHan.Framework.Primitives.Manifests;

namespace XiHan.BasicApp.Plugins.Operations.Plugins;

/// <summary>
/// 插件的列表项
/// </summary>
/// <param name="Id">主键</param>
/// <param name="ManifestId">清单 ID</param>
/// <param name="Name">短名</param>
/// <param name="Version">当前版本号</param>
/// <param name="Publisher">发布者</param>
/// <param name="Isolation">隔离级别</param>
/// <param name="IsEnabled">是否启用</param>
/// <param name="Remark">备注</param>
/// <param name="ConcurrencyStamp">并发戳：授权时原样交回</param>
/// <param name="CreatedAt">安装时间（UTC）</param>
/// <param name="ModifiedAt">修改时间（UTC）</param>
public sealed record PluginResponse(
    Guid Id,
    string ManifestId,
    string Name,
    string Version,
    string? Publisher,
    PluginIsolation Isolation,
    bool IsEnabled,
    string? Remark,
    string ConcurrencyStamp,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt)
{
    /// <summary>
    /// 从实体投影
    /// </summary>
    /// <param name="plugin">插件</param>
    public static PluginResponse From(SysPlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        return new PluginResponse(plugin.Id, plugin.ManifestId, plugin.Name, plugin.Version, plugin.Publisher, plugin.Isolation, plugin.IsEnabled, plugin.Remark, plugin.ConcurrencyStamp, plugin.CreatedAt, plugin.ModifiedAt);
    }
}

/// <summary>
/// 插件定义的一个权限码
/// </summary>
/// <param name="Code">权限码</param>
/// <param name="Scope">作用侧</param>
/// <param name="Audited">该权限保护的操作是否进审计</param>
public sealed record PluginPermissionResponse(string Code, PermissionScope Scope, bool Audited);

/// <summary>
/// 插件贡献的一个页面
/// </summary>
/// <param name="Code">页面码</param>
/// <param name="Route">路由</param>
/// <param name="TitleKey">标题文案键</param>
/// <param name="Icon">图标名</param>
/// <param name="Order">同级排序</param>
/// <param name="Permission">进入页面所需的权限码，为空只要求登录</param>
public sealed record PluginPageResponse(string Code, string Route, string TitleKey, string? Icon, int Order, string? Permission);

/// <summary>
/// 插件的贡献
/// </summary>
/// <param name="Operations">操作 ID</param>
/// <param name="Permissions">定义的权限码</param>
/// <param name="Pages">页面</param>
/// <param name="Callbacks">回调宿主操作要持有的宿主权限码</param>
public sealed record PluginContributionsResponse(
    IReadOnlyList<string> Operations,
    IReadOnlyList<PluginPermissionResponse> Permissions,
    IReadOnlyList<PluginPageResponse> Pages,
    IReadOnlyList<string> Callbacks)
{
    /// <summary>
    /// 从两份清单投影
    /// </summary>
    /// <param name="manifest">框架清单</param>
    /// <param name="descriptor">旁挂清单</param>
    public static PluginContributionsResponse From(Manifest manifest, PluginDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(descriptor);
        return new PluginContributionsResponse(
            [.. manifest.Contributes.Operations.Order(StringComparer.Ordinal)],
            [.. descriptor.Permissions.Select(static p => new PluginPermissionResponse(p.Code, p.Scope, p.Audited)).OrderBy(static p => p.Code, StringComparer.Ordinal)],
            [.. descriptor.Pages.Select(static p => new PluginPageResponse(p.Code, p.Route, p.TitleKey, p.Icon, p.Order, p.Permission)).OrderBy(static p => p.Order).ThenBy(static p => p.Code, StringComparer.Ordinal)],
            [.. descriptor.Callbacks.Order(StringComparer.Ordinal)]);
    }
}

/// <summary>
/// 插件包的核对结果
/// </summary>
/// <param name="ManifestId">清单 ID</param>
/// <param name="Name">短名</param>
/// <param name="Version">版本号</param>
/// <param name="Publisher">发布者</param>
/// <param name="Isolation">隔离级别</param>
/// <param name="Sha256">包的 SHA-256</param>
/// <param name="Size">包的字节数</param>
/// <param name="Contributions">贡献</param>
/// <param name="RequiredGrants">必须全部批准的授予</param>
/// <param name="OptionalGrants">可以另外批准的授予</param>
public sealed record PluginInspectionResponse(
    string ManifestId,
    string Name,
    string Version,
    string? Publisher,
    PluginIsolation Isolation,
    string Sha256,
    long Size,
    PluginContributionsResponse Contributions,
    IReadOnlyList<string> RequiredGrants,
    IReadOnlyList<string> OptionalGrants)
{
    /// <summary>
    /// 从核对结果投影
    /// </summary>
    /// <param name="inspection">核对结果</param>
    public static PluginInspectionResponse From(PluginInspection inspection)
    {
        ArgumentNullException.ThrowIfNull(inspection);
        var package = inspection.Package;
        return new PluginInspectionResponse(
            package.Manifest.Id,
            package.Descriptor.Name,
            package.Manifest.Version,
            package.Descriptor.Publisher,
            package.Manifest.Isolation ?? PluginIsolation.OutOfProcess,
            package.Sha256,
            package.Size,
            PluginContributionsResponse.From(package.Manifest, package.Descriptor),
            inspection.RequiredGrants,
            inspection.OptionalGrants);
    }
}

/// <summary>
/// 插件的一个版本
/// </summary>
/// <param name="Id">主键</param>
/// <param name="Version">版本号</param>
/// <param name="FileId">包文件</param>
/// <param name="Sha256">包的 SHA-256</param>
/// <param name="Size">包的字节数</param>
/// <param name="IsCurrent">是否是当前版本</param>
/// <param name="CreatedAt">上传时间（UTC）</param>
public sealed record PluginVersionResponse(Guid Id, string Version, Guid FileId, string Sha256, long Size, bool IsCurrent, DateTimeOffset CreatedAt);

/// <summary>
/// 插件的详情
/// </summary>
/// <param name="Plugin">插件</param>
/// <param name="Contributions">当前版本的贡献</param>
/// <param name="RequiredGrants">当前版本必须全部批准的授予</param>
/// <param name="OptionalGrants">可以另外批准的授予</param>
/// <param name="Grants">已批准的授予</param>
/// <param name="Versions">全部版本，新的在前</param>
public sealed record PluginDetailResponse(
    PluginResponse Plugin,
    PluginContributionsResponse Contributions,
    IReadOnlyList<string> RequiredGrants,
    IReadOnlyList<string> OptionalGrants,
    IReadOnlyList<string> Grants,
    IReadOnlyList<PluginVersionResponse> Versions);

/// <summary>
/// <c>plugins.get</c> 的请求
/// </summary>
/// <param name="Id">插件 ID</param>
public sealed record GetPluginRequest(Guid Id);

/// <summary>
/// <c>plugins.inspect</c> 的请求
/// </summary>
/// <param name="FileId">已上传的插件包文件（平台一侧）</param>
public sealed record InspectPluginRequest(Guid FileId);

/// <summary>
/// <c>plugins.install</c> 的请求
/// </summary>
/// <param name="FileId">已上传的插件包文件（平台一侧）</param>
/// <param name="Grants">批准的授予：须含核对给出的全部必须项，另外只能有可选项</param>
/// <param name="Remark">备注</param>
public sealed record InstallPluginRequest(
    Guid FileId,
    [Required, MaxLength(200)] IReadOnlyList<string> Grants,
    [StringLength(500)] string? Remark = null);

/// <summary>
/// <c>plugins.grant</c> 的请求
/// </summary>
/// <param name="Id">插件 ID</param>
/// <param name="ConcurrencyStamp">读到的并发戳</param>
/// <param name="Grants">批准的授予：整组替换，须含全部必须项，另外只能有可选项</param>
public sealed record GrantPluginRequest(
    Guid Id,
    [Required, StringLength(64, MinimumLength = 1)] string ConcurrencyStamp,
    [Required, MaxLength(200)] IReadOnlyList<string> Grants);

/// <summary>
/// <c>plugins.delete</c> 的请求：集合入参
/// </summary>
/// <param name="Ids">插件 ID，1 到 100 个</param>
public sealed record PluginIdsRequest([Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids);
