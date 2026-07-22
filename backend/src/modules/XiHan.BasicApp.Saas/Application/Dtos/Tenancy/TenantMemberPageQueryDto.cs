// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
    /// 所属租户主键
    /// </summary>
    /// <remarks>
    /// 查"某个租户有哪些成员"时<b>必须</b>传。平台管理员没有租户上下文，全局租户过滤器在平台态是放行全部的，
    /// 不显式按租户过滤就会把<b>所有租户</b>的成员关系一并捞出来。
    /// </remarks>
    public long? TenantId { get; set; }

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
