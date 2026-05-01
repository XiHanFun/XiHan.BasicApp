#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionChangeLogListItemDto
// Guid:853d85d5-9bec-421a-84cd-65c2030c5976
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 权限变更日志列表项 DTO
/// </summary>
public class PermissionChangeLogListItemDto : BasicAppDto
{
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
    public PermissionChangeType ChangeType { get; set; }

    /// <summary>
    /// 链路追踪 ID
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// 变更时间
    /// </summary>
    public DateTimeOffset ChangeTime { get; set; }

    /// <summary>
    /// 是否包含原因说明
    /// </summary>
    public bool HasReasonNote { get; set; }

    /// <summary>
    /// 是否包含变更摘要
    /// </summary>
    public bool HasChangeSummary { get; set; }

    /// <summary>
    /// 是否包含操作上下文
    /// </summary>
    public bool HasOperationContext { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }
}
