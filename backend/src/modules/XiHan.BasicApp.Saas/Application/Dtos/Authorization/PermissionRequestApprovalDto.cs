// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
