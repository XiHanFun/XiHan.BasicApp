// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 租户成员添加 DTO
/// </summary>
/// <remarks>
/// 直接把已有用户加入租户，成员关系立即生效（邀请状态为已接受）；
/// 走待接受流程请用 <see cref="TenantMemberInviteDto"/>。
/// </remarks>
public sealed class TenantMemberAddDto
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
    /// 生效时间（为空表示立即生效）
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
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
