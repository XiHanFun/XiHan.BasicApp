// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Settings.Domain.Services;

namespace XiHan.BasicApp.Settings.Operations.Site;

/// <summary>
/// <c>site.get</c> 的请求：没有输入
/// </summary>
public sealed record GetSiteRequest;

/// <summary>
/// <c>site.resolve</c> 的请求
/// </summary>
/// <param name="Host">前端访问的主机名（可以带端口）：没登录时按它找绑定了这个域名的租户</param>
public sealed record ResolveSiteRequest([StringLength(260)] string? Host = null);

/// <summary>
/// <c>site.update</c> 的请求
/// </summary>
/// <remarks>
/// 整体覆盖调用方这一侧的设置：文字为空即不设（租户沿用平台的），图片只能保留现在的或去掉，换图走 <c>POST /api/site/assets/{slot}</c>
/// </remarks>
/// <param name="Name">系统名</param>
/// <param name="Subtitle">登录页的标语</param>
/// <param name="Description">登录页的说明</param>
/// <param name="Logo">Logo：现在的文件 ID 即保留，为空即去掉</param>
/// <param name="Favicon">浏览器图标：现在的文件 ID 即保留，为空即去掉</param>
/// <param name="LoginBackground">登录页背景图：现在的文件 ID 即保留，为空即去掉</param>
/// <param name="CopyrightOwner">页脚的版权方</param>
/// <param name="CopyrightUrl">版权方的链接（http 或 https）</param>
/// <param name="CopyrightSince">版权起始年份</param>
/// <param name="Icp">备案号</param>
/// <param name="IcpUrl">备案号的链接（http 或 https）</param>
public sealed record UpdateSiteRequest(
    [StringLength(SiteSettings.TextMaxLength)] string? Name = null,
    [StringLength(SiteSettings.TextMaxLength)] string? Subtitle = null,
    [StringLength(SiteSettings.LongTextMaxLength)] string? Description = null,
    Guid? Logo = null,
    Guid? Favicon = null,
    Guid? LoginBackground = null,
    [StringLength(SiteSettings.TextMaxLength)] string? CopyrightOwner = null,
    [StringLength(SiteSettings.LongTextMaxLength)] string? CopyrightUrl = null,
    [StringLength(SiteSettings.TextMaxLength)] string? CopyrightSince = null,
    [StringLength(SiteSettings.TextMaxLength)] string? Icp = null,
    [StringLength(SiteSettings.LongTextMaxLength)] string? IcpUrl = null)
{
    /// <summary>
    /// 换成设置
    /// </summary>
    public SiteSettings ToSettings() => new(Name, Subtitle, Description, Logo, Favicon, LoginBackground, CopyrightOwner, CopyrightUrl, CopyrightSince, Icp, IcpUrl);
}

/// <summary>
/// 一张站点图片
/// </summary>
/// <param name="Id">文件 ID</param>
/// <param name="Url">匿名可读的地址（站内相对路径）</param>
public sealed record SiteAssetResponse(Guid Id, string Url)
{
    /// <summary>
    /// 匿名读站点图片的地址
    /// </summary>
    public static string UrlOf(Guid id) => $"/api/site/assets/{id:D}";

    /// <summary>
    /// 由文件 ID 构造
    /// </summary>
    public static SiteAssetResponse? From(Guid? id) => id is { } value ? new SiteAssetResponse(value, UrlOf(value)) : null;
}

/// <summary>
/// 站点设置的内容
/// </summary>
/// <param name="Name">系统名</param>
/// <param name="Subtitle">登录页的标语</param>
/// <param name="Description">登录页的说明</param>
/// <param name="Logo">Logo</param>
/// <param name="Favicon">浏览器图标</param>
/// <param name="LoginBackground">登录页背景图</param>
/// <param name="CopyrightOwner">页脚的版权方</param>
/// <param name="CopyrightUrl">版权方的链接</param>
/// <param name="CopyrightSince">版权起始年份</param>
/// <param name="Icp">备案号</param>
/// <param name="IcpUrl">备案号的链接</param>
public sealed record SiteProfileResponse(
    string? Name,
    string? Subtitle,
    string? Description,
    SiteAssetResponse? Logo,
    SiteAssetResponse? Favicon,
    SiteAssetResponse? LoginBackground,
    string? CopyrightOwner,
    string? CopyrightUrl,
    string? CopyrightSince,
    string? Icp,
    string? IcpUrl)
{
    /// <summary>
    /// 由设置构造
    /// </summary>
    public static SiteProfileResponse From(SiteSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return new(settings.Name, settings.Subtitle, settings.Description, SiteAssetResponse.From(settings.Logo), SiteAssetResponse.From(settings.Favicon), SiteAssetResponse.From(settings.LoginBackground), settings.CopyrightOwner, settings.CopyrightUrl, settings.CopyrightSince, settings.Icp, settings.IcpUrl);
    }
}

/// <summary>
/// 设置页看到的站点设置
/// </summary>
/// <param name="IsPlatform">调用方是不是平台：平台改的是站点设置，租户改的是自己的品牌</param>
/// <param name="Own">这一侧自己存着的（租户没设的字段为空）</param>
/// <param name="Inherited">租户沿用的平台设置；平台为空</param>
public sealed record SiteSettingsResponse(bool IsPlatform, SiteProfileResponse Own, SiteProfileResponse? Inherited);

/// <summary>
/// 前端壳层用的生效设置
/// </summary>
/// <param name="Profile">生效的设置：租户的叠在平台的之上</param>
/// <param name="TenantId">没登录时按域名找到的租户（登录页预填）；登录了或没找到为空</param>
public sealed record ResolvedSiteResponse(SiteProfileResponse Profile, string? TenantId);
