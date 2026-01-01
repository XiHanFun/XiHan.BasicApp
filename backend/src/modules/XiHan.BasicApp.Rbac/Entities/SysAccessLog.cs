#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysAccessLog
// Guid:0d28152c-d6e9-4396-addb-b479254bad50
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/8/14 6:25:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统访问日志实体
/// </summary>
[SugarTable("Sys_Access_Log", "系统访问日志表")]
[SplitTable(SplitType.Month)]
[SugarIndex("IX_SysAccessLog_UserId", nameof(UserId), OrderByType.Asc)]
[SugarIndex("IX_SysAccessLog_AccessResult", nameof(AccessResult), OrderByType.Asc)]
[SugarIndex("IX_SysAccessLog_AccessTime", nameof(AccessTime), OrderByType.Desc)]
[SugarIndex("IX_SysAccessLog_ResourcePath", nameof(ResourcePath), OrderByType.Asc)]
[SugarIndex("IX_SysAccessLog_TenantId", nameof(TenantId), OrderByType.Asc)]
public partial class SysAccessLog : RbacFullAuditedEntity<XiHanBasicAppIdType>
{
    /// <summary>
    /// 租户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "租户ID", IsNullable = true)]
    public virtual XiHanBasicAppIdType? TenantId { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "用户ID", IsNullable = true)]
    public virtual XiHanBasicAppIdType? UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    [SugarColumn(ColumnDescription = "用户名", Length = 50, IsNullable = true)]
    public virtual string? UserName { get; set; }

    /// <summary>
    /// 会话ID
    /// </summary>
    [SugarColumn(ColumnDescription = "会话ID", Length = 100, IsNullable = true)]
    public virtual string? SessionId { get; set; }

    /// <summary>
    /// 访问资源路径
    /// </summary>
    [SugarColumn(ColumnDescription = "访问资源路径", Length = 500, IsNullable = false)]
    public virtual string ResourcePath { get; set; } = string.Empty;

    /// <summary>
    /// 资源名称
    /// </summary>
    [SugarColumn(ColumnDescription = "资源名称", Length = 200, IsNullable = true)]
    public virtual string? ResourceName { get; set; }

    /// <summary>
    /// 资源类型
    /// </summary>
    [SugarColumn(ColumnDescription = "资源类型", Length = 50, IsNullable = true)]
    public virtual string? ResourceType { get; set; }

    /// <summary>
    /// 请求方法
    /// </summary>
    [SugarColumn(ColumnDescription = "请求方法", Length = 10, IsNullable = true)]
    public virtual string? Method { get; set; }

    /// <summary>
    /// 访问结果
    /// </summary>
    [SugarColumn(ColumnDescription = "访问结果")]
    public virtual AccessResult AccessResult { get; set; } = AccessResult.Success;

    /// <summary>
    /// 响应状态码
    /// </summary>
    [SugarColumn(ColumnDescription = "响应状态码")]
    public virtual int StatusCode { get; set; } = 200;

    /// <summary>
    /// 访问IP
    /// </summary>
    [SugarColumn(ColumnDescription = "访问IP", Length = 50, IsNullable = true)]
    public virtual string? AccessIp { get; set; }

    /// <summary>
    /// 访问地址
    /// </summary>
    [SugarColumn(ColumnDescription = "访问地址", Length = 200, IsNullable = true)]
    public virtual string? AccessLocation { get; set; }

    /// <summary>
    /// User-Agent
    /// </summary>
    [SugarColumn(ColumnDescription = "User-Agent", Length = 500, IsNullable = true)]
    public virtual string? UserAgent { get; set; }

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
    /// 设备类型
    /// </summary>
    [SugarColumn(ColumnDescription = "设备类型", Length = 50, IsNullable = true)]
    public virtual string? Device { get; set; }

    /// <summary>
    /// 访问来源
    /// </summary>
    [SugarColumn(ColumnDescription = "访问来源", Length = 500, IsNullable = true)]
    public virtual string? Referer { get; set; }

    /// <summary>
    /// 响应时间（毫秒）
    /// </summary>
    [SugarColumn(ColumnDescription = "响应时间（毫秒）")]
    public virtual XiHanBasicAppIdType ResponseTime { get; set; } = 0;

    /// <summary>
    /// 响应大小（字节）
    /// </summary>
    [SugarColumn(ColumnDescription = "响应大小（字节）")]
    public virtual XiHanBasicAppIdType ResponseSize { get; set; } = 0;

    /// <summary>
    /// 访问时间
    /// </summary>
    [SugarColumn(ColumnDescription = "访问时间")]
    public virtual DateTimeOffset AccessTime { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// 离开时间
    /// </summary>
    [SugarColumn(ColumnDescription = "离开时间", IsNullable = true)]
    public virtual DateTimeOffset? LeaveTime { get; set; }

    /// <summary>
    /// 停留时长（秒）
    /// </summary>
    [SugarColumn(ColumnDescription = "停留时长（秒）")]
    public virtual XiHanBasicAppIdType StayTime { get; set; } = 0;

    /// <summary>
    /// 错误信息
    /// </summary>
    [SugarColumn(ColumnDescription = "错误信息", Length = 1000, IsNullable = true)]
    public virtual string? ErrorMessage { get; set; }

    /// <summary>
    /// 扩展数据（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDescription = "扩展数据", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? ExtendData { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(IsNullable = false, ColumnDescription = "创建时间")]
    [SplitField]
    public override DateTimeOffset CreatedTime { get; set; }
}
