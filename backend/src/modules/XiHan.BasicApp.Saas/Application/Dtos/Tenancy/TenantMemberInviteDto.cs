// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 租户成员邀请 DTO
/// </summary>
/// <remarks>
/// 创建待接受的成员关系（邀请状态为待处理），被邀请人接受后才生效；
/// 需要立即生效请用 <see cref="TenantMemberAddDto"/>。
/// </remarks>
public sealed class TenantMemberInviteDto
{
    /// <summary>
    /// 所属租户主键
    /// </summary>
    public long TenantId { get; set; }

    /// <summary>
    /// 用户主键（平台上已存在的用户）
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 成员类型
    /// </summary>
    public TenantMemberType MemberType { get; set; } = TenantMemberType.Member;

    /// <summary>
    /// 生效时间（为空表示接受后立即生效）
    /// </summary>
    public DateTimeOffset? EffectiveTime { get; set; }

    /// <summary>
    /// 失效时间（为空表示永不过期）
    /// </summary>
    public DateTimeOffset? ExpirationTime { get; set; }

    /// <summary>
    /// 租户内显示名
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// 邀请备注
    /// </summary>
    public string? InviteRemark { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
