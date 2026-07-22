// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 用户部门归属状态更新 DTO
/// </summary>
public sealed class UserDepartmentStatusUpdateDto : BasicAppUDto
{
    /// <summary>
    /// 状态
    /// </summary>
    public ValidityStatus Status { get; set; } = ValidityStatus.Valid;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
