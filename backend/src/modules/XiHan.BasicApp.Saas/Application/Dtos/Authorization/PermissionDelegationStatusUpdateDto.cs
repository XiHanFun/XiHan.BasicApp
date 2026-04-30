#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionDelegationStatusUpdateDto
// Guid:d2bfdeea-cfa9-4760-a253-09a051f30d25
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
