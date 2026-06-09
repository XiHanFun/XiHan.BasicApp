#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserRoleListItemDto
// Guid:70bb9cb7-e19e-4e95-89b0-85c773497c10
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 用户角色列表项 DTO
/// </summary>
public sealed class UserRoleListItemDto : BasicAppDto
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
    /// 角色主键
    /// </summary>
    public long RoleId { get; set; }

    /// <summary>
    /// 角色编码
    /// </summary>
    public string? RoleCode { get; set; }

    /// <summary>
    /// 角色名称
    /// </summary>
    public string? RoleName { get; set; }

    /// <summary>
    /// 角色类型
    /// </summary>
    public RoleType? RoleType { get; set; }

    /// <summary>
    /// 是否全局角色
    /// </summary>
    public bool? IsGlobalRole { get; set; }

    /// <summary>
    /// 角色数据权限范围
    /// </summary>
    public DataPermissionScope? RoleDataScope { get; set; }

    /// <summary>
    /// 角色状态
    /// </summary>
    public EnableStatus? RoleStatus { get; set; }

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
