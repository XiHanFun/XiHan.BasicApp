// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.OpenPlatform.Domain.Entities;

/// <summary>
/// OAuth 应用：接入本系统单点登录的第三方应用。
/// </summary>
/// <remarks>
/// 平台库实体。平台在「应用管理」里建的没有归属人；用户在个人中心「我的应用」里建的归属本人，平台管理页也看得到（4.x 平台页看不到租户用户建的应用，ClientId 查重也只查平台的）。
/// 发给它的用户令牌只用于身份（受众是 ClientId，只认 userinfo 与 id_token，后台接口不认）。机密客户端的密钥只存 SHA-256（密钥是 256 位随机数，不需要慢散列；4.x 用 60 万次 PBKDF2，令牌端点可被当成耗 CPU 的入口）；公开客户端没有密钥、授权码必须带 PKCE。
/// </remarks>
[ActiveUniqueIndex(nameof(ClientId))]
[SugarIndex("ix_{table}_owner_user_id", nameof(OwnerUserId), OrderByType.Asc)]
public sealed class SysOAuthApp : AppAuditedEntity
{
    /// <summary>
    /// 应用名称。
    /// </summary>
    [StringLength(100)]
    public string AppName { get; set; } = string.Empty;

    /// <summary>
    /// 应用描述。
    /// </summary>
    [StringLength(500)]
    public string? AppDescription { get; set; }

    /// <summary>
    /// 客户端标识（全局唯一）。
    /// </summary>
    [StringLength(100)]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// 客户端类型。
    /// </summary>
    public OAuthClientType ClientType { get; set; } = OAuthClientType.Confidential;

    /// <summary>
    /// 客户端密钥的 SHA-256（小写十六进制）。
    /// </summary>
    /// <remarks>
    /// 公开客户端为空。
    /// </remarks>
    [StringLength(64)]
    public string? ClientSecretHash { get; set; }

    /// <summary>
    /// 应用类型（只用于展示）。
    /// </summary>
    /// <remarks>
    /// 4.x 同。
    /// </remarks>
    public OAuthAppType AppType { get; set; } = OAuthAppType.Web;

    /// <summary>
    /// 授权类型（空格分隔）。
    /// </summary>
    [StringLength(200)]
    public string GrantTypes { get; set; } = string.Empty;

    /// <summary>
    /// 回调地址（换行分隔，授权时逐字匹配）。
    /// </summary>
    [StringLength(2048)]
    public string RedirectUris { get; set; } = string.Empty;

    /// <summary>
    /// 授权范围（空格分隔）。
    /// </summary>
    [StringLength(200)]
    public string Scopes { get; set; } = string.Empty;

    /// <summary>
    /// 访问令牌有效期（秒）。
    /// </summary>
    public int AccessTokenLifetime { get; set; } = 3600;

    /// <summary>
    /// 刷新令牌有效期（秒）。
    /// </summary>
    public int RefreshTokenLifetime { get; set; } = 2592000;

    /// <summary>
    /// 授权码有效期（秒）。
    /// </summary>
    public int AuthorizationCodeLifetime { get; set; } = 300;

    /// <summary>
    /// Logo 地址。
    /// </summary>
    [StringLength(500)]
    public string? Logo { get; set; }

    /// <summary>
    /// 应用主页。
    /// </summary>
    [StringLength(500)]
    public string? Homepage { get; set; }

    /// <summary>
    /// 是否跳过授权同意页。
    /// </summary>
    public bool SkipConsent { get; set; }

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 归属人（个人中心「我的应用」建的）。
    /// </summary>
    /// <remarks>
    /// 平台建的为空。
    /// </remarks>
    public Guid? OwnerUserId { get; set; }

    /// <summary>
    /// 归属人的登录名（建立时的快照，平台管理页展示用）。
    /// </summary>
    [StringLength(256)]
    public string? OwnerUserName { get; set; }

    /// <summary>
    /// 备注。
    /// </summary>
    [StringLength(500)]
    public string? Remark { get; set; }
}

/// <summary>
/// OAuth 客户端类型。
/// </summary>
public enum OAuthClientType
{
    /// <summary>
    /// 机密客户端：有密钥，用于服务端应用。
    /// </summary>
    Confidential = 0,

    /// <summary>
    /// 公开客户端：没有密钥，用 PKCE 保护授权码（SPA、移动端、桌面端）。
    /// </summary>
    Public = 1,
}

/// <summary>
/// OAuth 应用类型（只用于展示）。
/// </summary>
/// <remarks>
/// 4.x 同值。
/// </remarks>
public enum OAuthAppType
{
    /// <summary>
    /// 网页应用。
    /// </summary>
    Web = 0,

    /// <summary>
    /// 移动应用。
    /// </summary>
    Mobile = 1,

    /// <summary>
    /// 桌面应用。
    /// </summary>
    Desktop = 2,

    /// <summary>
    /// 服务。
    /// </summary>
    Service = 3,
}
