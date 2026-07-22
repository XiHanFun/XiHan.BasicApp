// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 用户设置场景
/// </summary>
/// <remarks>
/// 用于在 (用户 × 设置键) 之上再加一层场景维度，支撑全场景设置同步：
/// 同一用户在不同场景下可以复用相同的设置键（如页面设置以 pageCode 为键），互不冲突。
/// </remarks>
public enum UserSettingScene
{
    /// <summary>
    /// 偏好设置（主题/外观/布局/组件/快捷键等全局应用偏好，设置键固定）
    /// </summary>
    [Description("偏好设置")]
    Preference = 0,

    /// <summary>
    /// 页面设置（列表页列设置/视图/搜索/分页快照等，按 pageCode 为设置键区分）
    /// </summary>
    [Description("页面设置")]
    Page = 1,
}
