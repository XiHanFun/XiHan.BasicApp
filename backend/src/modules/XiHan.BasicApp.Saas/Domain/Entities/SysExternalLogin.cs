#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysExternalLogin
// Guid:a1b2c3d4-5e6f-7890-abcd-ef1234567810
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/02 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统第三方登录绑定实体
/// </summary>
[SugarTable("Sys_External_Login", "系统第三方登录绑定表")]
[SugarIndex("UX_SysExternalLogin_Pr_PrKe_TeId", nameof(Provider), OrderByType.Asc, nameof(ProviderKey), OrderByType.Asc, nameof(TenantId), OrderByType.Asc, true)]
[SugarIndex("IX_SysExternalLogin_UsId", nameof(UserId), OrderByType.Asc)]
public partial class SysExternalLogin : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 关联的内部用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "用户ID", IsNullable = false)]
    public virtual long UserId { get; set; }

    /// <summary>
    /// 提供商名称（google、github、qq 等）
    /// </summary>
    [SugarColumn(ColumnDescription = "提供商名称", Length = 50, IsNullable = false)]
    public virtual string Provider { get; set; } = string.Empty;

    /// <summary>
    /// 提供商用户唯一标识
    /// </summary>
    [SugarColumn(ColumnDescription = "提供商用户标识", Length = 200, IsNullable = false)]
    public virtual string ProviderKey { get; set; } = string.Empty;

    /// <summary>
    /// 提供商显示名称
    /// </summary>
    [SugarColumn(ColumnDescription = "提供商显示名称", Length = 200, IsNullable = true)]
    public virtual string? ProviderDisplayName { get; set; }

    /// <summary>
    /// 三方邮箱
    /// </summary>
    [SugarColumn(ColumnDescription = "三方邮箱", Length = 200, IsNullable = true)]
    public virtual string? Email { get; set; }

    /// <summary>
    /// 三方头像
    /// </summary>
    [SugarColumn(ColumnDescription = "三方头像", Length = 500, IsNullable = true)]
    public virtual string? AvatarUrl { get; set; }

    /// <summary>
    /// 最后登录时间
    /// </summary>
    [SugarColumn(ColumnDescription = "最后登录时间", IsNullable = true)]
    public virtual DateTimeOffset? LastLoginTime { get; set; }
}
