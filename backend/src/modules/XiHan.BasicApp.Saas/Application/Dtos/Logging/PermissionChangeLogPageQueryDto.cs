#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionChangeLogPageQueryDto
// Guid:1ff8ef33-868e-4653-a1ef-40b022e3ba20
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 权限变更日志分页查询 DTO
/// </summary>
public sealed class PermissionChangeLogPageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 关键字
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 操作人主键
    /// </summary>
    public long? OperatorUserId { get; set; }

    /// <summary>
    /// 目标用户主键
    /// </summary>
    public long? TargetUserId { get; set; }

    /// <summary>
    /// 目标角色主键
    /// </summary>
    public long? TargetRoleId { get; set; }

    /// <summary>
    /// 权限主键
    /// </summary>
    public long? PermissionId { get; set; }

    /// <summary>
    /// 变更类型
    /// </summary>
    public PermissionChangeType? ChangeType { get; set; }

    /// <summary>
    /// 链路追踪 ID
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// 变更开始时间
    /// </summary>
    public DateTimeOffset? ChangeTimeStart { get; set; }

    /// <summary>
    /// 变更结束时间
    /// </summary>
    public DateTimeOffset? ChangeTimeEnd { get; set; }
}
