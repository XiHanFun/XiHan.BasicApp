#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUser
// Guid:2b28152c-d6e9-4396-addb-b479254bad0c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/8/14 2:18:56
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Entities.Base;
using XiHan.BasicApp.Core;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统用户实体
/// </summary>
[SugarTable("Sys_User", "系统用户表")]
[SugarIndex("IX_SysUser_UserName", nameof(UserName), OrderByType.Asc, true)]
[SugarIndex("IX_SysUser_Email", nameof(Email), OrderByType.Asc)]
[SugarIndex("IX_SysUser_Phone", nameof(Phone), OrderByType.Asc)]
[SugarIndex("IX_SysUser_TenantId", nameof(TenantId), OrderByType.Asc)]
public partial class SysUser : RbacFullAuditedEntity<long>
{
    /// <summary>
    /// 租户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "租户ID", IsNullable = true)]
    public virtual long? TenantId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    [SugarColumn(ColumnDescription = "用户名", Length = 50, IsNullable = false)]
    public virtual string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    [SugarColumn(ColumnDescription = "密码", Length = 100, IsNullable = false)]
    public virtual string Password { get; set; } = string.Empty;

    /// <summary>
    /// 真实姓名
    /// </summary>
    [SugarColumn(ColumnDescription = "真实姓名", Length = 50, IsNullable = true)]
    public virtual string? RealName { get; set; }

    /// <summary>
    /// 昵称
    /// </summary>
    [SugarColumn(ColumnDescription = "昵称", Length = 50, IsNullable = true)]
    public virtual string? NickName { get; set; }

    /// <summary>
    /// 头像
    /// </summary>
    [SugarColumn(ColumnDescription = "头像", Length = 500, IsNullable = true)]
    public virtual string? Avatar { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    [SugarColumn(ColumnDescription = "邮箱", Length = 100, IsNullable = true)]
    public virtual string? Email { get; set; }

    /// <summary>
    /// 手机号
    /// </summary>
    [SugarColumn(ColumnDescription = "手机号", Length = 20, IsNullable = true)]
    public virtual string? Phone { get; set; }

    /// <summary>
    /// 性别
    /// </summary>
    [SugarColumn(ColumnDescription = "性别")]
    public virtual UserGender Gender { get; set; } = UserGender.Unknown;

    /// <summary>
    /// 生日
    /// </summary>
    [SugarColumn(ColumnDescription = "生日", IsNullable = true)]
    public virtual DateTimeOffset? Birthday { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnDescription = "状态")]
    public virtual YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 最后登录时间
    /// </summary>
    [SugarColumn(ColumnDescription = "最后登录时间", IsNullable = true)]
    public virtual DateTimeOffset? LastLoginTime { get; set; }

    /// <summary>
    /// 最后登录IP
    /// </summary>
    [SugarColumn(ColumnDescription = "最后登录IP", Length = 50, IsNullable = true)]
    public virtual string? LastLoginIp { get; set; }

    /// <summary>
    /// 时区
    /// </summary>
    [SugarColumn(ColumnDescription = "时区", Length = 50, IsNullable = true)]
    public virtual string? TimeZone { get; set; }

    /// <summary>
    /// 语言
    /// </summary>
    [SugarColumn(ColumnDescription = "语言", Length = 10, IsNullable = true)]
    public virtual string? Language { get; set; } = "zh-CN";

    /// <summary>
    /// 国家/地区
    /// </summary>
    [SugarColumn(ColumnDescription = "国家/地区", Length = 50, IsNullable = true)]
    public virtual string? Country { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
