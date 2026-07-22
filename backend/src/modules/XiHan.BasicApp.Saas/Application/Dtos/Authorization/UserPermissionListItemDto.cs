// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 用户直授权限列表项 DTO
/// </summary>
public sealed class UserPermissionListItemDto : BasicAppDto
{
    /// <summary>
    /// 用户主键
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 租户成员主键
    /// </summary>
    public long? TenantMemberId { get; set; }

    /// <summary>
    /// 租户内显示名
    /// </summary>
    public string? TenantMemberDisplayName { get; set; }

    /// <summary>
    /// 成员类型
    /// </summary>
    public TenantMemberType? TenantMemberType { get; set; }

    /// <summary>
    /// 成员邀请状态
    /// </summary>
    public TenantMemberInviteStatus? TenantMemberInviteStatus { get; set; }

    /// <summary>
    /// 成员状态
    /// </summary>
    public ValidityStatus? TenantMemberStatus { get; set; }

    /// <summary>
    /// 权限主键
    /// </summary>
    public long PermissionId { get; set; }

    /// <summary>
    /// 权限编码
    /// </summary>
    public string? PermissionCode { get; set; }

    /// <summary>
    /// 权限名称
    /// </summary>
    public string? PermissionName { get; set; }

    /// <summary>
    /// 权限类型
    /// </summary>
    public PermissionType? PermissionType { get; set; }

    /// <summary>
    /// 模块编码
    /// </summary>
    public string? ModuleCode { get; set; }

    /// <summary>
    /// 是否全局权限
    /// </summary>
    public bool? IsGlobalPermission { get; set; }

    /// <summary>
    /// 是否要求审计
    /// </summary>
    public bool? IsRequireAudit { get; set; }

    /// <summary>
    /// 权限状态
    /// </summary>
    public EnableStatus? PermissionStatus { get; set; }

    /// <summary>
    /// 权限操作
    /// </summary>
    public PermissionAction PermissionAction { get; set; }

    /// <summary>
    /// 生效时间
    /// </summary>
    public DateTimeOffset? EffectiveTime { get; set; }

    /// <summary>
    /// 失效时间
    /// </summary>
    public DateTimeOffset? ExpirationTime { get; set; }

    /// <summary>
    /// 授权原因
    /// </summary>
    public string? GrantReason { get; set; }

    /// <summary>
    /// 绑定状态
    /// </summary>
    public ValidityStatus Status { get; set; }

    /// <summary>
    /// 是否已过期
    /// </summary>
    public bool IsExpired { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }
}
