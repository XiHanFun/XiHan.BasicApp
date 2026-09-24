// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 支持人员入驻 DTO（平台把平台账号以支持成员身份加入指定租户）
/// </summary>
public sealed class TenantSupportMemberAddDto
{
    /// <summary>
    /// 目标租户主键
    /// </summary>
    public long TenantId { get; set; }

    /// <summary>
    /// 平台账号的用户主键
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 生效时间（为空表示立即生效）
    /// </summary>
    public DateTimeOffset? EffectiveTime { get; set; }

    /// <summary>
    /// 失效时间（为空表示永不过期）
    /// </summary>
    public DateTimeOffset? ExpirationTime { get; set; }

    /// <summary>
    /// 备注（入驻事由）
    /// </summary>
    public string? Remark { get; set; }
}
