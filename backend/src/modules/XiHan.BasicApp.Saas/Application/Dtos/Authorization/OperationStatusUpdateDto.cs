#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OperationStatusUpdateDto
// Guid:1db8fe83-7f7f-414e-9f92-51a0ec2642c8
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 操作定义状态更新 DTO
/// </summary>
public sealed class OperationStatusUpdateDto : BasicAppUDto
{
    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus Status { get; set; } = EnableStatus.Enabled;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
