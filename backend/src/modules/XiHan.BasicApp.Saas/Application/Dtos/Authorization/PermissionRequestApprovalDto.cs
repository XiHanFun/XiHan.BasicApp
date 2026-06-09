#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionRequestApprovalDto
// Guid:6b1c2d3e-4f50-4a61-8b72-c3d4e5f6a7b8
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/07 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 权限申请审批 DTO（通过 / 驳回）
/// </summary>
public sealed class PermissionRequestApprovalDto : BasicAppUDto
{
    /// <summary>
    /// 审批备注 / 意见
    /// </summary>
    public string? Remark { get; set; }
}
