// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 权限申请状态更新 DTO
/// </summary>
public sealed class PermissionRequestStatusUpdateDto : BasicAppUDto
{
    /// <summary>
    /// 申请状态
    /// </summary>
    public PermissionRequestStatus RequestStatus { get; set; }

    /// <summary>
    /// 审批单主键
    /// </summary>
    public long? ReviewId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
