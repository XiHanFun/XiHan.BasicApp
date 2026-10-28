// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Identity.Domain.Entities;

/// <summary>
/// 用户设置的场景：一条设置由（用户, 场景, 设置键）唯一定位。
/// </summary>
public enum UserSettingScene
{
    /// <summary>
    /// 偏好与全局设置：应用偏好（主题、外观、布局、组件、快捷键）、收藏夹等，按设置键区分（如 <c>global</c>、<c>favorites</c>）。
    /// </summary>
    Preference = 0,

    /// <summary>
    /// 页面设置：列表页的列、搜索、个人视图等，设置键是页面编码。
    /// </summary>
    Page = 1,
}
