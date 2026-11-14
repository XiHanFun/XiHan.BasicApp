// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.OpenPlatform.Domain.Entities;

/// <summary>
/// OAuth 应用：接入本系统单点登录的第三方应用
/// </summary>
/// <remarks>
/// 接入本系统单点登录的第三方应用：平台在「应用管理」里建的没有归属人，用户在个人中心「我的应用」里建的归属本人，平台管理页也看得到
/// 落平台库、不分租户：令牌端点与 userinfo 绕过内核管道、没有租户上下文，也要能按 ClientId 找到应用
/// 发给它的用户令牌只用于身份（受众是 ClientId，只认 userinfo 与 id_token，后台接口不认）：授权类型只开授权码与刷新令牌，范围只开 <c>openid profile email phone</c>，没有用户的客户端凭证模式用不上
/// 机密客户端的密钥是 256 位随机数，只存 SHA-256、只在新建与重置时返回一次：随机密钥没有可猜的空间，不需要慢散列，慢散列反而让匿名的令牌端点成为耗 CPU 的入口
/// 公开客户端（SPA、移动端、桌面端）放不住密钥，所以没有密钥、授权码必须带 PKCE
/// 授权码、同意记录与令牌都按 ClientId 关联应用、不建外键，所以 ClientId 与客户端类型只在新建时定；ClientId 的唯一是部分唯一索引，应用软删除后同一个 ClientId 可以重建
/// 删除是软删除，授权码、同意与令牌不连带删：客户端认证与 userinfo 只认启用中的应用，应用删除或停用即让它发出的令牌失效，不用逐行吊销
/// 我的应用的授权类型、范围（<c>openid profile email</c>）与有效期固定，也不能跳过同意页：个人建的应用只拿身份，跳过同意只留给平台登记的应用
/// </remarks>
[SugarTable(TableName = "sys_oauth_app", TableDescription = "OAuth 应用：接入本系统单点登录的第三方应用")]
[ActiveUniqueIndex(nameof(ClientId), Name = "ux_sys_oauth_app_client_id")]
[SugarIndex("ix_sys_oauth_app_owner_user_id", nameof(OwnerUserId), OrderByType.Asc)]
public sealed class SysOAuthApp : AppAuditedEntity
{
    /// <summary>
    /// 应用名称
    /// </summary>
    [SugarColumn(ColumnName = "app_name", Length = 100, IsNullable = false, ColumnDescription = "应用名称")]
    public string AppName { get; set; } = string.Empty;

    /// <summary>
    /// 应用描述
    /// </summary>
    [SugarColumn(ColumnName = "app_description", Length = 500, IsNullable = true, ColumnDescription = "应用描述")]
    public string? AppDescription { get; set; }

    /// <summary>
    /// 客户端标识（全局唯一）
    /// </summary>
    [SugarColumn(ColumnName = "client_id", Length = 100, IsNullable = false, ColumnDescription = "客户端标识（全局唯一）")]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// 客户端类型
    /// </summary>
    [SugarColumn(ColumnName = "client_type", IsNullable = false, ColumnDescription = "客户端类型")]
    public OAuthClientType ClientType { get; set; } = OAuthClientType.Confidential;

    /// <summary>
    /// 客户端密钥的 SHA-256（小写十六进制）
    /// </summary>
    /// <remarks>
    /// 公开客户端为空
    /// </remarks>
    [SugarColumn(ColumnName = "client_secret_hash", Length = 64, IsNullable = true, ColumnDescription = "客户端密钥的 SHA-256（小写十六进制）")]
    public string? ClientSecretHash { get; set; }

    /// <summary>
    /// 应用类型（只用于展示）
    /// </summary>
    [SugarColumn(ColumnName = "app_type", IsNullable = false, ColumnDescription = "应用类型（只用于展示）")]
    public OAuthAppType AppType { get; set; } = OAuthAppType.Web;

    /// <summary>
    /// 授权类型（空格分隔）
    /// </summary>
    [SugarColumn(ColumnName = "grant_types", Length = 200, IsNullable = false, ColumnDescription = "授权类型（空格分隔）")]
    public string GrantTypes { get; set; } = string.Empty;

    /// <summary>
    /// 回调地址（换行分隔，授权时逐字匹配）
    /// </summary>
    [SugarColumn(ColumnName = "redirect_uris", Length = 2048, IsNullable = false, ColumnDescription = "回调地址（换行分隔，授权时逐字匹配）")]
    public string RedirectUris { get; set; } = string.Empty;

    /// <summary>
    /// 授权范围（空格分隔）
    /// </summary>
    [SugarColumn(ColumnName = "scopes", Length = 200, IsNullable = false, ColumnDescription = "授权范围（空格分隔）")]
    public string Scopes { get; set; } = string.Empty;

    /// <summary>
    /// 访问令牌有效期（秒）
    /// </summary>
    [SugarColumn(ColumnName = "access_token_lifetime", IsNullable = false, ColumnDescription = "访问令牌有效期（秒）")]
    public int AccessTokenLifetime { get; set; } = 3600;

    /// <summary>
    /// 刷新令牌有效期（秒）
    /// </summary>
    [SugarColumn(ColumnName = "refresh_token_lifetime", IsNullable = false, ColumnDescription = "刷新令牌有效期（秒）")]
    public int RefreshTokenLifetime { get; set; } = 2592000;

    /// <summary>
    /// 授权码有效期（秒）
    /// </summary>
    [SugarColumn(ColumnName = "authorization_code_lifetime", IsNullable = false, ColumnDescription = "授权码有效期（秒）")]
    public int AuthorizationCodeLifetime { get; set; } = 300;

    /// <summary>
    /// Logo 地址
    /// </summary>
    [SugarColumn(ColumnName = "logo", Length = 500, IsNullable = true, ColumnDescription = "Logo 地址")]
    public string? Logo { get; set; }

    /// <summary>
    /// 应用主页
    /// </summary>
    [SugarColumn(ColumnName = "homepage", Length = 500, IsNullable = true, ColumnDescription = "应用主页")]
    public string? Homepage { get; set; }

    /// <summary>
    /// 是否跳过授权同意页
    /// </summary>
    [SugarColumn(ColumnName = "skip_consent", IsNullable = false, ColumnDescription = "是否跳过授权同意页")]
    public bool SkipConsent { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [SugarColumn(ColumnName = "is_enabled", IsNullable = false, ColumnDescription = "是否启用")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 归属人（个人中心「我的应用」建的）
    /// </summary>
    /// <remarks>
    /// 平台建的为空
    /// </remarks>
    [SugarColumn(ColumnName = "owner_user_id", IsNullable = true, ColumnDescription = "归属人（个人中心「我的应用」建的）")]
    public Guid? OwnerUserId { get; set; }

    /// <summary>
    /// 归属人的登录名（建立时的快照，平台管理页展示用）
    /// </summary>
    [SugarColumn(ColumnName = "owner_user_name", Length = 256, IsNullable = true, ColumnDescription = "归属人的登录名（建立时的快照，平台管理页展示用）")]
    public string? OwnerUserName { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "remark", Length = 500, IsNullable = true, ColumnDescription = "备注")]
    public string? Remark { get; set; }
}

/// <summary>
/// OAuth 客户端类型
/// </summary>
public enum OAuthClientType
{
    /// <summary>
    /// 机密客户端：有密钥，用于服务端应用
    /// </summary>
    Confidential = 0,

    /// <summary>
    /// 公开客户端：没有密钥，用 PKCE 保护授权码（SPA、移动端、桌面端）
    /// </summary>
    Public = 1,
}

/// <summary>
/// OAuth 应用类型（只用于展示）
/// </summary>
public enum OAuthAppType
{
    /// <summary>
    /// 网页应用
    /// </summary>
    Web = 0,

    /// <summary>
    /// 移动应用
    /// </summary>
    Mobile = 1,

    /// <summary>
    /// 桌面应用
    /// </summary>
    Desktop = 2,

    /// <summary>
    /// 服务
    /// </summary>
    Service = 3,
}
