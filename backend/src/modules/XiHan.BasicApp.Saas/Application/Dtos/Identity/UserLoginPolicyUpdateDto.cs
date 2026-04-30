#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserLoginPolicyUpdateDto
// Guid:d6802af0-1780-40ad-b8ad-06906bdf2731
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 用户登录策略更新 DTO
/// </summary>
public sealed class UserLoginPolicyUpdateDto
{
    /// <summary>
    /// 用户主键
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 是否允许多端登录
    /// </summary>
    public bool AllowMultiLogin { get; set; }

    /// <summary>
    /// 最大登录设备数（0 表示不限制）
    /// </summary>
    public int MaxLoginDevices { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
