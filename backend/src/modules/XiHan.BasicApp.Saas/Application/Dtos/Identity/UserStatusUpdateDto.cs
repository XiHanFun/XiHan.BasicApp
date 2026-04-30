#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserStatusUpdateDto
// Guid:ff92ac1c-603f-40a6-8ed4-92295f18339f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 用户状态更新 DTO
/// </summary>
public sealed class UserStatusUpdateDto
{
    /// <summary>
    /// 用户主键
    /// </summary>
    public long BasicId { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus Status { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
