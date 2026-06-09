#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserSetting.Enum
// Guid:3f1b9c20-5d84-4a6e-9b21-7c0e4d8f2a16
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/10 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
