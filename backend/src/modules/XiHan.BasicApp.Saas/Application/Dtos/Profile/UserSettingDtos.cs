// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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

    /// <summary>
    /// 变更来源标记（不落库，原样随实时推送转发给该用户的其它设备）
    /// </summary>
    /// <remarks>
    /// 前端用它携带主题切换的点击位置（视口百分比），使其它设备的扩散动画从相同相对位置展开。
    /// 服务端不解释其内容，仅做中转。
    /// </remarks>
    public string? Origin { get; set; }
}
