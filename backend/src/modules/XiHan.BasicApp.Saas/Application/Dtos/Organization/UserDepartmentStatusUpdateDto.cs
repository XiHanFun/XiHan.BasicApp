#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserDepartmentStatusUpdateDto
// Guid:785131b7-8f26-43ef-a4a6-d707f3c7fcf3
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
