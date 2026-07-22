// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 会话角色分页查询 DTO
/// </summary>
public sealed class SessionRolePageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 会话主键
    /// </summary>
    public long? SessionId { get; set; }

    /// <summary>
    /// 角色主键
    /// </summary>
    public long? RoleId { get; set; }

    /// <summary>
    /// 会话角色状态
    /// </summary>
    public SessionRoleStatus? Status { get; set; }

    /// <summary>
    /// 激活开始时间
    /// </summary>
    public DateTimeOffset? ActivatedTimeStart { get; set; }

    /// <summary>
    /// 激活结束时间
    /// </summary>
    public DateTimeOffset? ActivatedTimeEnd { get; set; }

    /// <summary>
    /// 过期开始时间
    /// </summary>
    public DateTimeOffset? ExpirationTimeStart { get; set; }

    /// <summary>
    /// 过期结束时间
    /// </summary>
    public DateTimeOffset? ExpirationTimeEnd { get; set; }
}
