#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserSettingDtos
// Guid:8d4f0125-6e70-4c1d-8f4b-3a5e9c2d7b84
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/10 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 用户设置 DTO
/// </summary>
public sealed class UserSettingDto
{
    /// <summary>
    /// 设置场景
    /// </summary>
    public UserSettingScene Scene { get; set; } = UserSettingScene.Preference;

    /// <summary>
    /// 设置键
    /// </summary>
    public string SettingKey { get; set; } = string.Empty;

    /// <summary>
    /// 设置载荷（JSON；无则为空）
    /// </summary>
    public string? SettingValue { get; set; }
}

/// <summary>
/// 保存用户设置入参
/// </summary>
public sealed class UserSettingSaveDto
{
    /// <summary>
    /// 设置场景
    /// </summary>
    public UserSettingScene Scene { get; set; } = UserSettingScene.Preference;

    /// <summary>
    /// 设置键
    /// </summary>
    public string SettingKey { get; set; } = string.Empty;

    /// <summary>
    /// 设置载荷（JSON）
    /// </summary>
    public string? SettingValue { get; set; }

    /// <summary>
    /// 发起端标识（每端会话唯一，不落库；随变更推送原样回传，供发起端过滤自身回显）
    /// </summary>
    public string? ClientId { get; set; }
}
