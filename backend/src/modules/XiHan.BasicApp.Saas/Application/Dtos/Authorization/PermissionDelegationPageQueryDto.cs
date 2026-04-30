#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionDelegationPageQueryDto
// Guid:dbd514c6-f027-4cdf-bb3c-202b672e4735
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 权限委托分页查询 DTO
/// </summary>
public sealed class PermissionDelegationPageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 关键字（委托原因、备注）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 委托人用户主键
    /// </summary>
    public long? DelegatorUserId { get; set; }

    /// <summary>
    /// 被委托人用户主键
    /// </summary>
    public long? DelegateeUserId { get; set; }

    /// <summary>
    /// 权限主键
    /// </summary>
    public long? PermissionId { get; set; }

    /// <summary>
    /// 角色主键
    /// </summary>
    public long? RoleId { get; set; }

    /// <summary>
    /// 委托状态
    /// </summary>
    public DelegationStatus? DelegationStatus { get; set; }
}
