// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Identity.Domain.Entities;

/// <summary>
/// 第三方账号绑定
/// </summary>
/// <remarks>
/// 平台库，跟着账号走。在（注册地, 提供商, 第三方标识）上唯一
/// 提供商存规范的类型名（<c>github</c>、<c>wechat</c>…，不存配置的方案名：同一个微信 unionid 经两个方案仍是一个身份）。不存第三方的令牌
/// </remarks>
[SugarTable(TableName = "sys_user_external_login", TableDescription = "第三方账号绑定")]
[ActiveUniqueIndex(nameof(HomeTenantId), nameof(Provider), nameof(ProviderKey), Name = "ux_sys_user_external_login_home_tenant_id_provider_provider_key")]
[ActiveUniqueIndex(nameof(UserId), nameof(Provider), Name = "ux_sys_user_external_login_user_id_provider")]
[DiffLogIgnored]
public sealed class SysUserExternalLogin : AppAuditedEntity
{
    /// <summary>
    /// 用户
    /// </summary>
    [SugarColumn(ColumnName = "user_id", IsNullable = false, ColumnDescription = "用户")]
    public Guid UserId { get; set; }

    /// <summary>
    /// 用户的注册地（平台账号为 <c>platform</c>）
    /// </summary>
    [SugarColumn(ColumnName = "home_tenant_id", Length = 64, IsNullable = false, ColumnDescription = "用户的注册地（平台账号为 platform）")]
    public string HomeTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 提供商类型
    /// </summary>
    [SugarColumn(ColumnName = "provider", Length = 32, IsNullable = false, ColumnDescription = "提供商类型")]
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// 第三方的用户标识
    /// </summary>
    [SugarColumn(ColumnName = "provider_key", Length = 200, IsNullable = false, ColumnDescription = "第三方的用户标识")]
    public string ProviderKey { get; set; } = string.Empty;

    /// <summary>
    /// 第三方的显示名（每次登录刷新）
    /// </summary>
    [SugarColumn(ColumnName = "display_name", Length = 200, IsNullable = true, ColumnDescription = "第三方的显示名（每次登录刷新）")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// 第三方的邮箱（每次登录刷新）
    /// </summary>
    [SugarColumn(ColumnName = "email", Length = 200, IsNullable = true, ColumnDescription = "第三方的邮箱（每次登录刷新）")]
    public string? Email { get; set; }

    /// <summary>
    /// 第三方的头像（每次登录刷新）
    /// </summary>
    [SugarColumn(ColumnName = "avatar_url", Length = 500, IsNullable = true, ColumnDescription = "第三方的头像（每次登录刷新）")]
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// 最近一次用它登录的时间
    /// </summary>
    [SugarColumn(ColumnName = "last_login_at", IsNullable = true, ColumnDescription = "最近一次用它登录的时间")]
    public DateTimeOffset? LastLoginAt { get; set; }
}
