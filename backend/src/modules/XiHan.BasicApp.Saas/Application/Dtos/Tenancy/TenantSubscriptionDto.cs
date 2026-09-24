// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 当前租户的订阅（租户自己看：版本套餐、到期时间、席位与存储用量）
/// </summary>
public sealed class TenantSubscriptionDto
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
    /// 租户状态
    /// </summary>
    public TenantStatus TenantStatus { get; set; }

    /// <summary>
    /// 到期时间（null 表示长期有效）
    /// </summary>
    public DateTimeOffset? ExpirationTime { get; set; }

    /// <summary>
    /// 是否已到期
    /// </summary>
    public bool IsExpired { get; set; }

    /// <summary>
    /// 版本编码（未绑定版本时为 null）
    /// </summary>
    public string? EditionCode { get; set; }

    /// <summary>
    /// 版本名称（未绑定版本时为 null）
    /// </summary>
    public string? EditionName { get; set; }

    /// <summary>
    /// 版本说明
    /// </summary>
    public string? EditionDescription { get; set; }

    /// <summary>
    /// 是否免费版
    /// </summary>
    public bool IsFreeEdition { get; set; }

    /// <summary>
    /// 生效席位上限（租户未设值时取版本的，null 表示不限）
    /// </summary>
    public int? EffectiveUserLimit { get; set; }

    /// <summary>
    /// 已占用席位数（不含支持人员）
    /// </summary>
    public long UsedUserCount { get; set; }

    /// <summary>
    /// 生效存储上限(MB)（租户未设值时取版本的，null 表示不限）
    /// </summary>
    public long? EffectiveStorageLimit { get; set; }

    /// <summary>
    /// 已占用存储(字节)
    /// </summary>
    public long UsedStorageBytes { get; set; }
}
