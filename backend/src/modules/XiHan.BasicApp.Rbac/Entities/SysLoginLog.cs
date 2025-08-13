#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysLoginLog
// Guid:4d28152c-d6e9-4396-addb-b479254bad24
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/8/14 3:55:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Data.SqlSugar.Entities;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统登录日志实体
/// </summary>
[SugarTable("sys_login_log", "系统登录日志表")]
public partial class SysLoginLog : SugarEntityWithAudit<long>
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "用户ID", IsNullable = false)]
    public virtual long UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    [SugarColumn(ColumnDescription = "用户名", Length = 50, IsNullable = false)]
    public virtual string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 登录IP
    /// </summary>
    [SugarColumn(ColumnDescription = "登录IP", Length = 50, IsNullable = true)]
    public virtual string? LoginIp { get; set; }

    /// <summary>
    /// 登录地址
    /// </summary>
    [SugarColumn(ColumnDescription = "登录地址", Length = 200, IsNullable = true)]
    public virtual string? LoginLocation { get; set; }

    /// <summary>
    /// 浏览器类型
    /// </summary>
    [SugarColumn(ColumnDescription = "浏览器类型", Length = 100, IsNullable = true)]
    public virtual string? Browser { get; set; }

    /// <summary>
    /// 操作系统
    /// </summary>
    [SugarColumn(ColumnDescription = "操作系统", Length = 100, IsNullable = true)]
    public virtual string? Os { get; set; }

    /// <summary>
    /// 登录状态
    /// </summary>
    [SugarColumn(ColumnDescription = "登录状态")]
    public virtual YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 登录消息
    /// </summary>
    [SugarColumn(ColumnDescription = "登录消息", Length = 500, IsNullable = true)]
    public virtual string? Message { get; set; }

    /// <summary>
    /// 登录时间
    /// </summary>
    [SugarColumn(ColumnDescription = "登录时间")]
    public virtual DateTimeOffset LoginTime { get; set; } = DateTimeOffset.Now;
}
