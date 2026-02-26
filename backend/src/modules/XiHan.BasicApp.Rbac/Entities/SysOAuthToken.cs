#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysOAuthToken
// Guid:7c28152c-d6e9-4396-addb-b479254bad31
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 05:30:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统 OAuth 令牌实体
/// </summary>
[SugarTable("Sys_OAuth_Token", "系统 OAuth 令牌表")]
[SugarIndex("UX_SysOAuthToken_AcTo", nameof(AccessToken), OrderByType.Asc, true)]
[SugarIndex("IX_SysOAuthToken_ReTo", nameof(RefreshToken), OrderByType.Asc)]
[SugarIndex("IX_SysOAuthToken_ClId", nameof(ClientId), OrderByType.Asc)]
[SugarIndex("IX_SysOAuthToken_UsId", nameof(UserId), OrderByType.Asc)]
[SugarIndex("IX_SysOAuthToken_IsRe", nameof(IsRevoked), OrderByType.Asc)]
[SugarIndex("IX_SysOAuthToken_AcToExTi", nameof(AccessTokenExpiresTime), OrderByType.Asc)]
public partial class SysOAuthToken : RbacCreationEntity<long>
{
    /// <summary>
    /// 访问令牌（敏感信息）
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(ColumnDescription = "访问令牌", Length = 1000, IsNullable = false)]
    public virtual string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// 刷新令牌（敏感信息）
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
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
    public virtual long? UserId { get; set; }

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
    /// 状态
    /// </summary>
    [SugarColumn(ColumnDescription = "状态", IsNullable = true)]
    public virtual YesOrNo? State { get; set; }

    /// <summary>
    /// 访问令牌过期时间
    /// </summary>
    [SugarColumn(ColumnDescription = "访问令牌过期时间")]
    public virtual DateTimeOffset AccessTokenExpiresTime { get; set; }

    /// <summary>
    /// 刷新令牌过期时间
    /// </summary>
    [SugarColumn(ColumnDescription = "刷新令牌过期时间", IsNullable = true)]
    public virtual DateTimeOffset? RefreshTokenExpiresTime { get; set; }

    /// <summary>
    /// 是否已撤销
    /// </summary>
    [SugarColumn(ColumnDescription = "是否已撤销")]
    public virtual bool IsRevoked { get; set; } = false;

    /// <summary>
    /// 撤销时间
    /// </summary>
    [SugarColumn(ColumnDescription = "撤销时间", IsNullable = true)]
    public virtual DateTimeOffset? RevokedTime { get; set; }
}
