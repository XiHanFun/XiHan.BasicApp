// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 租户成员状态更新 DTO
/// </summary>
public sealed class TenantMemberStatusUpdateDto
{
    /// <summary>
    /// 租户成员主键
    /// </summary>
    public long BasicId { get; set; }

    /// <summary>
    /// 成员状态
    /// </summary>
    public ValidityStatus Status { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
