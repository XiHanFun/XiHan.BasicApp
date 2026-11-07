// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.Identity.Domain.Entities;

/// <summary>
/// 用户设置，供前端跨端同步偏好、收藏与页面设置
/// </summary>
/// <remarks>
/// 平台库实体，跟着账号走，不分租户。按（用户, 场景, 设置键）存一段 JSON
/// 后端不解释 <see cref="Value"/> 的语义，由前端按版本兼容解析；同一键整体覆盖写入
/// </remarks>
[ActiveUniqueIndex(nameof(UserId), nameof(Scene), nameof(Key))]
[DiffLogIgnored]
public sealed class SysUserSetting : AppAuditedEntity
{
    /// <summary>
    /// 用户
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 场景
    /// </summary>
    public UserSettingScene Scene { get; set; } = UserSettingScene.Preference;

    /// <summary>
    /// 设置键：偏好场景是固定标识（如 <c>global</c>），页面场景是页面编码
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 设置载荷（JSON）
    /// </summary>
    [LongText]
    public string? Value { get; set; }
}

/// <summary>
/// 用户设置的场景：一条设置由（用户, 场景, 设置键）唯一定位
/// </summary>
public enum UserSettingScene
{
    /// <summary>
    /// 偏好与全局设置
    /// </summary>
    /// <remarks>
    /// 应用偏好（主题、外观、布局、组件、快捷键）、收藏夹等，按设置键区分（如 <c>global</c>、<c>favorites</c>）
    /// </remarks>
    Preference = 0,

    /// <summary>
    /// 页面设置：列表页的列、搜索、个人视图等，设置键是页面编码
    /// </summary>
    Page = 1,
}
