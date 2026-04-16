#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysNotification
// Guid:ac28152c-d6e9-4396-addb-b479254bad34
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 05:45:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统通知实体（通知内容主表，每用户投递状态存储在 SysUserNotification）
/// </summary>
[SugarTable("SysNotification", "系统通知表")]
[SugarIndex("IX_{table}_TeId", nameof(TenantId), OrderByType.Asc)]
[SugarIndex("IX_{table}_CrTi", nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_MoTi", nameof(ModifiedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_MoId", nameof(ModifiedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_IsDe", nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("IX_{table}_NoTy", nameof(NotificationType), OrderByType.Asc)]
[SugarIndex("IX_{table}_SeTi", nameof(SendTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_IsPu", nameof(IsPublished), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_SeTi", nameof(TenantId), OrderByType.Asc, nameof(SendTime), OrderByType.Desc)]
public partial class SysNotification : BasicAppAggregateRoot
{
    /// <summary>
    /// 发送用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "发送用户ID", IsNullable = true)]
    public virtual long? SendUserId { get; set; }

    /// <summary>
    /// 通知类型
    /// </summary>
    [SugarColumn(ColumnDescription = "通知类型")]
    public virtual NotificationType NotificationType { get; set; } = NotificationType.System;

    /// <summary>
    /// 通知标题
    /// </summary>
    [SugarColumn(ColumnDescription = "通知标题", Length = 200, IsNullable = false)]
    public virtual string Title { get; set; } = string.Empty;

    /// <summary>
    /// 通知内容
    /// </summary>
    [SugarColumn(ColumnDescription = "通知内容", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? Content { get; set; }

    /// <summary>
    /// 通知图标
    /// </summary>
    [SugarColumn(ColumnDescription = "通知图标", Length = 100, IsNullable = true)]
    public virtual string? Icon { get; set; }

    /// <summary>
    /// 跳转链接
    /// </summary>
    [SugarColumn(ColumnDescription = "跳转链接", Length = 500, IsNullable = true)]
    public virtual string? Link { get; set; }

    /// <summary>
    /// 业务类型
    /// </summary>
    [SugarColumn(ColumnDescription = "业务类型", Length = 50, IsNullable = true)]
    public virtual string? BusinessType { get; set; }

    /// <summary>
    /// 业务ID
    /// </summary>
    [SugarColumn(ColumnDescription = "业务ID", IsNullable = true)]
    public virtual long? BusinessId { get; set; }

    /// <summary>
    /// 发送时间
    /// </summary>
    [SugarColumn(ColumnDescription = "发送时间")]
    public virtual DateTimeOffset SendTime { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// 过期时间
    /// </summary>
    [SugarColumn(ColumnDescription = "过期时间", IsNullable = true)]
    public virtual DateTimeOffset? ExpireTime { get; set; }

    /// <summary>
    /// 是否全员通知
    /// </summary>
    [SugarColumn(ColumnDescription = "是否全员通知")]
    public virtual bool IsGlobal { get; set; } = false;

    /// <summary>
    /// 是否需要确认
    /// </summary>
    [SugarColumn(ColumnDescription = "是否需要确认")]
    public virtual bool NeedConfirm { get; set; } = false;

    /// <summary>
    /// 是否已发布（发布后不可编辑/删除）
    /// </summary>
    [SugarColumn(ColumnDescription = "是否已发布")]
    public virtual bool IsPublished { get; set; } = false;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
