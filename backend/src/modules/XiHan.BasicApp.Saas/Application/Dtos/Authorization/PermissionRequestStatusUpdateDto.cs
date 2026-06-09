#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionRequestStatusUpdateDto
// Guid:780caca9-ca39-414a-99b5-dae58c8d0a29
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
