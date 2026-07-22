// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 租户版本更新 DTO
/// </summary>
public sealed class TenantEditionUpdateDto : BasicAppUDto
{
    /// <summary>
    /// 版本名称
    /// </summary>
    public string EditionName { get; set; } = string.Empty;

    /// <summary>
    /// 版本描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 用户数限制
    /// </summary>
    public int? UserLimit { get; set; }

    /// <summary>
    /// 存储空间限制(MB)
    /// </summary>
    public long? StorageLimit { get; set; }

    /// <summary>
    /// 价格
    /// </summary>
    public decimal? Price { get; set; }

    /// <summary>
    /// 计费周期(月)
    /// </summary>
    public int? BillingPeriodMonths { get; set; }

    /// <summary>
    /// 是否免费版本
    /// </summary>
    public bool IsFree { get; set; }

    /// <summary>
    /// 是否默认版本
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
