#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionRequestPageQueryDto
// Guid:0480e03d-4982-49eb-a8f8-550f28b75be2
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
