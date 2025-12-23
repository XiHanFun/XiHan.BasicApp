#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysOAuthCode
// Guid:6c28152c-d6e9-4396-addb-b479254bad30
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/8/14 5:25:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities.Base;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统 OAuth 授权码实体
/// </summary>
[SugarTable("Sys_OAuth_Code", "系统 OAuth 授权码表")]
[SugarIndex("IX_SysOAuthCode_Code", nameof(Code), OrderByType.Asc, true)]
[SugarIndex("IX_SysOAuthCode_ClientId", nameof(ClientId), OrderByType.Asc)]
[SugarIndex("IX_SysOAuthCode_UserId", nameof(UserId), OrderByType.Asc)]
public partial class SysOAuthCode : RbacFullAuditedEntity<XiHanBasicAppIdType>
{
    /// <summary>
    /// 授权码
    /// </summary>
    [SugarColumn(ColumnDescription = "授权码", Length = 100, IsNullable = false)]
    public virtual string Code { get; set; } = string.Empty;

    /// <summary>
    /// 客户端ID
    /// </summary>
    [SugarColumn(ColumnDescription = "客户端ID", Length = 100, IsNullable = false)]
    public virtual string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "用户ID", IsNullable = false)]
    public virtual XiHanBasicAppIdType UserId { get; set; }

    /// <summary>
    /// 重定向URI
    /// </summary>
    [SugarColumn(ColumnDescription = "重定向URI", Length = 500, IsNullable = false)]
    public virtual string RedirectUri { get; set; } = string.Empty;

    /// <summary>
    /// 权限范围
    /// </summary>
    [SugarColumn(ColumnDescription = "权限范围", Length = 500, IsNullable = true)]
    public virtual string? Scopes { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnDescription = "状态", Length = 20, IsNullable = true)]
    public virtual string? State { get; set; }

    /// <summary>
    /// 质询码
    /// </summary>
    [SugarColumn(ColumnDescription = "质询码", Length = 100, IsNullable = true)]
    public virtual string? CodeChallenge { get; set; }

    /// <summary>
    /// 质询方法
    /// </summary>
    [SugarColumn(ColumnDescription = "质询方法", Length = 20, IsNullable = true)]
    public virtual string? CodeChallengeMethod { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    [SugarColumn(ColumnDescription = "过期时间")]
    public virtual DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// 是否已使用
    /// </summary>
    [SugarColumn(ColumnDescription = "是否已使用")]
    public virtual bool IsUsed { get; set; } = false;

    /// <summary>
    /// 使用时间
    /// </summary>
    [SugarColumn(ColumnDescription = "使用时间", IsNullable = true)]
    public virtual DateTimeOffset? UsedAt { get; set; }
}
