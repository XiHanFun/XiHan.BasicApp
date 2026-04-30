#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionDelegationListItemDto
// Guid:7740f680-af6c-4231-b6a6-e64d4d357391
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 权限委托列表项 DTO
/// </summary>
public sealed class PermissionDelegationListItemDto : BasicAppDto
{
    /// <summary>
    /// 委托人用户主键
    /// </summary>
    public long DelegatorUserId { get; set; }

    /// <summary>
    /// 委托人租户成员主键
    /// </summary>
    public long? DelegatorTenantMemberId { get; set; }

    /// <summary>
    /// 委托人租户内显示名
    /// </summary>
    public string? DelegatorDisplayName { get; set; }

    /// <summary>
    /// 被委托人用户主键
    /// </summary>
    public long DelegateeUserId { get; set; }

    /// <summary>
    /// 被委托人租户成员主键
    /// </summary>
    public long? DelegateeTenantMemberId { get; set; }

    /// <summary>
    /// 被委托人租户内显示名
    /// </summary>
    public string? DelegateeDisplayName { get; set; }

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
    /// 委托状态
    /// </summary>
    public DelegationStatus DelegationStatus { get; set; }

    /// <summary>
    /// 生效时间
    /// </summary>
    public DateTimeOffset? EffectiveTime { get; set; }

    /// <summary>
    /// 失效时间
    /// </summary>
    public DateTimeOffset ExpirationTime { get; set; }

    /// <summary>
    /// 是否已过期
    /// </summary>
    public bool IsExpired { get; set; }

    /// <summary>
    /// 委托原因
    /// </summary>
    public string? DelegationReason { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTimeOffset? ModifiedTime { get; set; }
}
