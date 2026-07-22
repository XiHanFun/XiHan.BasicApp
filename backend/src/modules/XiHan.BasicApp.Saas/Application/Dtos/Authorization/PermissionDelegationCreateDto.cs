// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 权限委托创建 DTO
/// </summary>
public sealed class PermissionDelegationCreateDto
{
    /// <summary>
    /// 委托人用户主键
    /// </summary>
    public long DelegatorUserId { get; set; }

    /// <summary>
    /// 被委托人用户主键
    /// </summary>
    public long DelegateeUserId { get; set; }

    /// <summary>
    /// 权限主键，为空表示委托全部权限
    /// </summary>
    public long? PermissionId { get; set; }

    /// <summary>
    /// 角色主键，为空表示不限角色
    /// </summary>
    public long? RoleId { get; set; }

    /// <summary>
    /// 生效时间，为空表示立即生效
    /// </summary>
    public DateTimeOffset? EffectiveTime { get; set; }

    /// <summary>
    /// 失效时间
    /// </summary>
    public DateTimeOffset ExpirationTime { get; set; }

    /// <summary>
    /// 委托原因
    /// </summary>
    public string? DelegationReason { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
