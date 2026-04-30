#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserPermissionDetailDto
// Guid:91eb0bc8-f1f4-4c40-98c6-0fe93356bffe
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
/// 用户直授权限详情 DTO
/// </summary>
public sealed class UserPermissionDetailDto : BasicAppDto
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
    /// 权限描述
    /// </summary>
    public string? PermissionDescription { get; set; }

    /// <summary>
    /// 权限类型
    /// </summary>
    public PermissionType? PermissionType { get; set; }

    /// <summary>
    /// 模块编码
    /// </summary>
    public string? ModuleCode { get; set; }

    /// <summary>
    /// 权限标签
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// 是否全局权限
    /// </summary>
    public bool? IsGlobalPermission { get; set; }

    /// <summary>
    /// 是否要求审计
    /// </summary>
    public bool? IsRequireAudit { get; set; }

    /// <summary>
    /// 权限优先级
    /// </summary>
    public int? PermissionPriority { get; set; }

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

    /// <summary>
    /// 创建人主键
    /// </summary>
    public long? CreatedId { get; set; }

    /// <summary>
    /// 创建人
    /// </summary>
    public string? CreatedBy { get; set; }
}
