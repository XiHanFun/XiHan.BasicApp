// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 租户成员更新 DTO
/// </summary>
public sealed class TenantMemberUpdateDto
{
    /// <summary>
    /// 租户成员主键
    /// </summary>
    public long BasicId { get; set; }

    /// <summary>
    /// 成员类型
    /// </summary>
    public TenantMemberType MemberType { get; set; }

    /// <summary>
    /// 生效时间
    /// </summary>
    public DateTimeOffset? EffectiveTime { get; set; }

    /// <summary>
    /// 失效时间
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
