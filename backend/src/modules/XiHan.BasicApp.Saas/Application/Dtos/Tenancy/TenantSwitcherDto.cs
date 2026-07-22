// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 租户切换项 DTO
/// </summary>
public sealed class TenantSwitcherDto
{
    /// <summary>
    /// 租户主键
    /// </summary>
    public long TenantId { get; set; }

    /// <summary>
    /// 租户编码
    /// </summary>
    public string TenantCode { get; set; } = string.Empty;

    /// <summary>
    /// 租户名称
    /// </summary>
    public string TenantName { get; set; } = string.Empty;

    /// <summary>
    /// 租户简称
    /// </summary>
    public string? TenantShortName { get; set; }

    /// <summary>
    /// Logo
    /// </summary>
    public string? Logo { get; set; }

    /// <summary>
    /// 域名
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    /// 租户状态
    /// </summary>
    public TenantStatus TenantStatus { get; set; }

    /// <summary>
    /// 配置状态
    /// </summary>
    public TenantConfigStatus ConfigStatus { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTimeOffset? ExpirationTime { get; set; }

    /// <summary>
    /// 租户成员关系主键
    /// </summary>
    public long MembershipId { get; set; }

    /// <summary>
    /// 成员类型
    /// </summary>
    public TenantMemberType MemberType { get; set; }

    /// <summary>
    /// 邀请状态
    /// </summary>
    public TenantMemberInviteStatus InviteStatus { get; set; }

    /// <summary>
    /// 成员身份失效时间
    /// </summary>
    public DateTimeOffset? MembershipExpirationTime { get; set; }

    /// <summary>
    /// 加入时间（受邀响应时间，缺省为成员关系创建时间）
    /// </summary>
    public DateTimeOffset JoinedTime { get; set; }

    /// <summary>
    /// 是否当前租户上下文
    /// </summary>
    public bool IsCurrent { get; set; }
}
