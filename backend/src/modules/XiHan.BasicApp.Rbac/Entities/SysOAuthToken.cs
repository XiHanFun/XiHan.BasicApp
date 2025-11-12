#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysOAuthToken
// Guid:7c28152c-d6e9-4396-addb-b479254bad31
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/8/14 5:30:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统 OAuth 令牌实体
/// </summary>
[SugarTable("sys_oauth_token", "系统 OAuth 令牌表")]
[SugarIndex("IX_SysOAuthToken_AccessToken", nameof(AccessToken), OrderByType.Asc, true)]
[SugarIndex("IX_SysOAuthToken_RefreshToken", nameof(RefreshToken), OrderByType.Asc)]
[SugarIndex("IX_SysOAuthToken_ClientId", nameof(ClientId), OrderByType.Asc)]
[SugarIndex("IX_SysOAuthToken_UserId", nameof(UserId), OrderByType.Asc)]
public partial class SysOAuthToken : RbacFullAuditedEntity<RbacIdType>
{
    /// <summary>
    /// 访问令牌
    /// </summary>
    [SugarColumn(ColumnDescription = "访问令牌", Length = 1000, IsNullable = false)]
    public virtual string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// 刷新令牌
    /// </summary>
    [SugarColumn(ColumnDescription = "刷新令牌", Length = 1000, IsNullable = true)]
    public virtual string? RefreshToken { get; set; }

    /// <summary>
    /// 令牌类型
    /// </summary>
    [SugarColumn(ColumnDescription = "令牌类型", Length = 20, IsNullable = false)]
    public virtual string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// 客户端ID
    /// </summary>
    [SugarColumn(ColumnDescription = "客户端ID", Length = 100, IsNullable = false)]
    public virtual string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "用户ID", IsNullable = true)]
    public virtual RbacIdType? UserId { get; set; }

    /// <summary>
    /// 授权类型
    /// </summary>
    [SugarColumn(ColumnDescription = "授权类型")]
    public virtual GrantType GrantType { get; set; } = GrantType.AuthorizationCode;

    /// <summary>
    /// 权限范围
    /// </summary>
    [SugarColumn(ColumnDescription = "权限范围", Length = 500, IsNullable = true)]
    public virtual string? Scopes { get; set; }

    /// <summary>
    /// 访问令牌过期时间
    /// </summary>
    [SugarColumn(ColumnDescription = "访问令牌过期时间")]
    public virtual DateTimeOffset AccessTokenExpiresAt { get; set; }

    /// <summary>
    /// 刷新令牌过期时间
    /// </summary>
    [SugarColumn(ColumnDescription = "刷新令牌过期时间", IsNullable = true)]
    public virtual DateTimeOffset? RefreshTokenExpiresAt { get; set; }

    /// <summary>
    /// 是否已撤销
    /// </summary>
    [SugarColumn(ColumnDescription = "是否已撤销")]
    public virtual bool IsRevoked { get; set; } = false;

    /// <summary>
    /// 撤销时间
    /// </summary>
    [SugarColumn(ColumnDescription = "撤销时间", IsNullable = true)]
    public virtual DateTimeOffset? RevokedAt { get; set; }
}
