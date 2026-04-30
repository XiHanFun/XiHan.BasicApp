#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserLockUpdateDto
// Guid:97d09a53-716d-4372-9e69-8e6b3e56c8dc
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 用户锁定状态更新 DTO
/// </summary>
public sealed class UserLockUpdateDto
{
    /// <summary>
    /// 用户主键
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 是否锁定
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// 锁定结束时间
    /// </summary>
    public DateTimeOffset? LockoutEndTime { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
