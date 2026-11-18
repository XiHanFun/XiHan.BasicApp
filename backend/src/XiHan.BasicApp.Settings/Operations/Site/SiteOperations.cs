// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.MultiTenancy;
using XiHan.BasicApp.Settings.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Operations.Site;

/// <summary>
/// 取站点设置
/// </summary>
/// <remarks>
/// <c>GET /api/site</c>。设置页用：平台看站点设置，租户看自己的品牌与沿用的平台设置
/// </remarks>
/// <param name="profiles">站点设置</param>
[Operation(Id, Expose = Expose.Http, Permission = SettingsPermissions.SiteRead)]
public sealed class GetSite(SiteProfiles profiles) : IOperation<GetSiteRequest, SiteSettingsResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "site.get";

    /// <inheritdoc />
    public async ValueTask<Result<SiteSettingsResponse>> HandleAsync(GetSiteRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return await SiteResponses.SettingsOfAsync(profiles, SettingsScope.OwnerOf(context), context.CancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// 改站点设置
/// </summary>
/// <remarks>
/// <c>PUT /api/site</c>。整体覆盖调用方这一侧：平台改站点设置，租户改自己的品牌（第一次保存时建租户的覆盖参数）
/// 图片只能保留现在的或去掉（去掉的随即回收），给别的文件 ID 即 <c>SETTINGS_VALIDATION_012</c>；换图走宿主的 <c>POST /api/site/assets/{slot}</c>
/// </remarks>
/// <param name="profiles">站点设置</param>
[Operation(Id, Expose = Expose.Http, Permission = SettingsPermissions.SiteUpdate)]
public sealed class UpdateSite(SiteProfiles profiles) : IOperation<UpdateSiteRequest, SiteSettingsResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "site.update";

    /// <inheritdoc />
    public async ValueTask<Result<SiteSettingsResponse>> HandleAsync(UpdateSiteRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var owner = SettingsScope.OwnerOf(context);
        var current = await profiles.OwnAsync(owner, context.CancellationToken).ConfigureAwait(false);
        if (!current.TryGetValue(out var own))
        {
            return current.Error!;
        }

        foreach (var (field, requested, kept) in new[] { ("logo", request.Logo, own.Logo), ("favicon", request.Favicon, own.Favicon) })
        {
            if (requested is { } id && id != kept)
            {
                return Error.Validation(SettingsErrors.SiteAssetUnknown, SettingsMessages.SiteAssetUnknown, MessageArguments.Of(("field", field), ("id", id)));
            }
        }

        var saved = await profiles.SaveAsync(owner, request.ToSettings(), context.CancellationToken).ConfigureAwait(false);
        return saved.TryGetValue(out var settings)
            ? await SiteResponses.SettingsOfAsync(profiles, owner, settings, context.CancellationToken).ConfigureAwait(false)
            : saved.Error!;
    }
}

/// <summary>
/// 取前端壳层用的站点设置
/// </summary>
/// <remarks>
/// 匿名只读操作，登记在架构测试的匿名白名单。<c>GET /api/site:resolve?host=</c>。登录了按令牌里的租户（平台主体即站点设置）；没登录按 <c>host</c> 找绑定了这个域名的租户，
/// 找到即返回它的品牌与租户 ID 供登录页预填，没找到即站点设置。只用于展示，不参与身份判定（租户只认令牌声明）
/// </remarks>
/// <param name="profiles">站点设置</param>
/// <param name="domains">按域名找租户（Tenancy 实现）</param>
[Operation(Id, ReadOnly = true, Expose = Expose.Http)]
public sealed class ResolveSite(SiteProfiles profiles, IEnumerable<ITenantDomainResolver> domains) : IOperation<ResolveSiteRequest, ResolvedSiteResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "site.resolve";

    /// <inheritdoc />
    public async ValueTask<Result<ResolvedSiteResponse>> HandleAsync(ResolveSiteRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        string? found = null;
        var owner = TenantIds.Platform;
        var security = context.GetSecurity();
        if (security.Principal.IsAuthenticated)
        {
            owner = SettingsScope.OwnerOf(context);
        }
        else if (!string.IsNullOrWhiteSpace(request.Host) && domains.FirstOrDefault() is { } resolver
            && (await resolver.FindTenantAsync(request.Host, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var tenant))
        {
            owner = found = tenant;
        }

        var effective = await profiles.EffectiveAsync(owner, context.CancellationToken).ConfigureAwait(false);
        return effective.TryGetValue(out var settings)
            ? new ResolvedSiteResponse(SiteProfileResponse.From(settings), found)
            : effective.Error!;
    }
}

/// <summary>
/// 站点设置的响应
/// </summary>
/// <remarks>
/// 操作与宿主的图片上传端点共用
/// </remarks>
public static class SiteResponses
{
    /// <summary>
    /// 设置页看到的：这一侧自己的，租户另带沿用的平台设置
    /// </summary>
    /// <param name="profiles">站点设置</param>
    /// <param name="owner">所属侧</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static async Task<Result<SiteSettingsResponse>> SettingsOfAsync(SiteProfiles profiles, string owner, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        var own = await profiles.OwnAsync(owner, cancellationToken).ConfigureAwait(false);
        return own.TryGetValue(out var settings)
            ? await SettingsOfAsync(profiles, owner, settings, cancellationToken).ConfigureAwait(false)
            : own.Error!;
    }

    /// <summary>
    /// 刚保存之后设置页看到的
    /// </summary>
    /// <remarks>
    /// 参数缓存要等提交之后才失效，同一次调用里再读会读到旧值，所以直接用刚保存的这一份
    /// </remarks>
    /// <param name="profiles">站点设置</param>
    /// <param name="owner">所属侧</param>
    /// <param name="settings">这一侧刚保存的设置</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static async Task<Result<SiteSettingsResponse>> SettingsOfAsync(SiteProfiles profiles, string owner, SiteSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(settings);
        if (SettingsScope.IsPlatform(owner))
        {
            return new SiteSettingsResponse(true, SiteProfileResponse.From(settings), null);
        }

        var platform = await profiles.OwnAsync(TenantIds.Platform, cancellationToken).ConfigureAwait(false);
        return platform.TryGetValue(out var inherited)
            ? new SiteSettingsResponse(false, SiteProfileResponse.From(settings), SiteProfileResponse.From(inherited))
            : platform.Error!;
    }
}
