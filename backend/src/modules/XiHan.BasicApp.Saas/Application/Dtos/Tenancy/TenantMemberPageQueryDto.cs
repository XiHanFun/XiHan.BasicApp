#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantMemberPageQueryDto
// Guid:9ea0095f-7296-47b7-93c8-f64d736ec681
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
/// 租户成员分页查询 DTO
/// </summary>
public sealed class TenantMemberPageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 关键字（租户内显示名、邀请备注、备注）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 用户主键
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// 成员类型
    /// </summary>
    public TenantMemberType? MemberType { get; set; }

    /// <summary>
    /// 邀请状态
    /// </summary>
    public TenantMemberInviteStatus? InviteStatus { get; set; }

    /// <summary>
    /// 成员状态
    /// </summary>
    public ValidityStatus? Status { get; set; }

    /// <summary>
    /// 失效开始时间
    /// </summary>
    public DateTimeOffset? ExpirationTimeStart { get; set; }

    /// <summary>
    /// 失效结束时间
    /// </summary>
    public DateTimeOffset? ExpirationTimeEnd { get; set; }
}
