// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 租户版本状态更新 DTO
/// </summary>
public sealed class TenantEditionStatusUpdateDto
{
    /// <summary>
    /// 租户版本主键
    /// </summary>
    public long BasicId { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus Status { get; set; }
}
