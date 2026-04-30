#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionRequestListItemDto
// Guid:6062d54b-3705-431c-98bb-ee515809d488
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 权限申请列表项 DTO
/// </summary>
public sealed class PermissionRequestListItemDto : BasicAppDto
{
    /// <summary>
    /// 申请人用户主键
    /// </summary>
    public long RequestUserId { get; set; }

    /// <summary>
    /// 申请人租户成员主键
    /// </summary>
    public long? RequestTenantMemberId { get; set; }

    /// <summary>
    /// 申请人租户内显示名
    /// </summary>
    public string? RequestUserDisplayName { get; set; }

    /// <summary>
    /// 权限主键
    /// </summary>
    public long? PermissionId { get; set; }

    /// <summary>
    /// 权限编码
    /// </summary>
    public string? PermissionCode { get; set; }

    /// <summary>
    /// 权限名称
    /// </summary>
    public string? PermissionName { get; set; }

    /// <summary>
    /// 角色主键
    /// </summary>
    public long? RoleId { get; set; }

    /// <summary>
    /// 角色编码
    /// </summary>
    public string? RoleCode { get; set; }

    /// <summary>
    /// 角色名称
    /// </summary>
    public string? RoleName { get; set; }

    /// <summary>
    /// 申请状态
    /// </summary>
    public PermissionRequestStatus RequestStatus { get; set; }

    /// <summary>
    /// 申请原因
    /// </summary>
    public string RequestReason { get; set; } = string.Empty;

    /// <summary>
    /// 期望生效时间
    /// </summary>
    public DateTimeOffset? ExpectedEffectiveTime { get; set; }

    /// <summary>
    /// 期望失效时间
    /// </summary>
    public DateTimeOffset? ExpectedExpirationTime { get; set; }

    /// <summary>
    /// 期望有效期是否已过期
    /// </summary>
    public bool IsExpectedExpired { get; set; }

    /// <summary>
    /// 审批单主键
    /// </summary>
    public long? ReviewId { get; set; }

    /// <summary>
    /// 审批单编码
    /// </summary>
    public string? ReviewCode { get; set; }

    /// <summary>
    /// 审批单标题
    /// </summary>
    public string? ReviewTitle { get; set; }

    /// <summary>
    /// 审批状态
    /// </summary>
    public AuditStatus? ReviewStatus { get; set; }

    /// <summary>
    /// 审批结果
    /// </summary>
    public AuditResult? ReviewResult { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTimeOffset? ModifiedTime { get; set; }
}
