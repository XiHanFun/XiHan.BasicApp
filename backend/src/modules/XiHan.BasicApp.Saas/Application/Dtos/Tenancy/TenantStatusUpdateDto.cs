// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 租户状态更新 DTO
/// </summary>
public sealed class TenantStatusUpdateDto
{
    /// <summary>
    /// 租户主键
    /// </summary>
    public long BasicId { get; set; }

    /// <summary>
    /// 租户状态
    /// </summary>
    public TenantStatus TenantStatus { get; set; }

    /// <summary>
    /// 变更原因
    /// </summary>
    public string? Reason { get; set; }
}
