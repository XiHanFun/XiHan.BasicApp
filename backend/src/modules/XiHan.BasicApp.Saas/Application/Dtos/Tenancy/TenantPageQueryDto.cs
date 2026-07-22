// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 租户分页查询 DTO
/// </summary>
public sealed class TenantPageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 关键字（租户编码、名称、简称、域名）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 租户状态
    /// </summary>
    public TenantStatus? TenantStatus { get; set; }

    /// <summary>
    /// 配置状态
    /// </summary>
    public TenantConfigStatus? ConfigStatus { get; set; }

    /// <summary>
    /// 版本/套餐主键
    /// </summary>
    public long? EditionId { get; set; }

    /// <summary>
    /// 到期开始时间
    /// </summary>
    public DateTimeOffset? ExpirationTimeStart { get; set; }

    /// <summary>
    /// 到期结束时间
    /// </summary>
    public DateTimeOffset? ExpirationTimeEnd { get; set; }
}
