// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 权限申请分页查询 DTO
/// </summary>
public sealed class PermissionRequestPageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 关键字（申请原因、备注）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 申请人用户主键
    /// </summary>
    public long? RequestUserId { get; set; }

    /// <summary>
    /// 权限主键
    /// </summary>
    public long? PermissionId { get; set; }

    /// <summary>
    /// 角色主键
    /// </summary>
    public long? RoleId { get; set; }

    /// <summary>
    /// 审批单主键
    /// </summary>
    public long? ReviewId { get; set; }

    /// <summary>
    /// 申请状态
    /// </summary>
    public PermissionRequestStatus? RequestStatus { get; set; }
}
