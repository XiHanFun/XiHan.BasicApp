#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantEditionListItemDto
// Guid:9c456025-8705-47ec-8a1d-81c15f6f03d7
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 租户版本列表项 DTO
/// </summary>
public sealed class TenantEditionListItemDto : BasicAppDto
{
    /// <summary>
    /// 版本编码
    /// </summary>
    public string EditionCode { get; set; } = string.Empty;

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
    /// 状态
    /// </summary>
    public EnableStatus Status { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTimeOffset? ModifiedTime { get; set; }
}
