// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 权限委托状态更新 DTO
/// </summary>
public sealed class PermissionDelegationStatusUpdateDto : BasicAppUDto
{
    /// <summary>
    /// 委托状态
    /// </summary>
    public DelegationStatus DelegationStatus { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
