#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SessionRolePageQueryDto
// Guid:62c369ea-fdc2-40b6-bbe9-79cb1afce5ae
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
    public DateTimeOffset? ActivatedAtStart { get; set; }

    /// <summary>
    /// 激活结束时间
    /// </summary>
    public DateTimeOffset? ActivatedAtEnd { get; set; }

    /// <summary>
    /// 过期开始时间
    /// </summary>
    public DateTimeOffset? ExpiresAtStart { get; set; }

    /// <summary>
    /// 过期结束时间
    /// </summary>
    public DateTimeOffset? ExpiresAtEnd { get; set; }
}
