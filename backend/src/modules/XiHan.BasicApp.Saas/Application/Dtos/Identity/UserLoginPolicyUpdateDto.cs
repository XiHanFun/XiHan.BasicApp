// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
