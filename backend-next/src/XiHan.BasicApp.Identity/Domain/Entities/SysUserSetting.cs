// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.Identity.Domain.Entities;

/// <summary>
/// 用户设置（平台库实体，跟着账号走，不分租户）：按（用户, 场景, 设置键）存一段 JSON，供前端跨端同步偏好、收藏与页面设置。
/// 后端不解释 <see cref="Value"/> 的语义，由前端按版本兼容解析；同一键整体覆盖写入。
/// </summary>
[DiffLogIgnored]
public sealed class SysUserSetting : AppAuditedEntity
{
    /// <summary>
    /// 用户。
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 场景。
    /// </summary>
    public UserSettingScene Scene { get; set; } = UserSettingScene.Preference;

    /// <summary>
    /// 设置键：偏好场景是固定标识（如 <c>global</c>），页面场景是页面编码。
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 设置载荷（JSON）。
    /// </summary>
    [LongText]
    public string? Value { get; set; }
}
