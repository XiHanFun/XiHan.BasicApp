// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.Identity.Domain.Entities;

/// <summary>
/// 用户设置，供前端跨端同步偏好、收藏与页面设置
/// </summary>
/// <remarks>
/// 前端的偏好、收藏与页面设置要跨端同步，按（用户, 场景, 设置键）存一段 JSON、整体覆盖，<see cref="Value"/> 由前端按版本兼容解析；后端只从偏好快照（场景偏好、键 <c>global</c>）里读界面语言 <c>xihan_locale</c>，给通知选语言
/// 跟着账号走、落平台库，不分租户：同一个人在哪一侧看到的都是同一份
/// 读穿共享缓存 10 分钟，没存过的也缓存成空值：前端每进一个页面都要读；写入即失效，提交后经实时连接把新值推给这个人的所有连接
/// 值为 <c>null</c> 即清空、行留着；部分唯一索引 <c>ux_sys_user_setting_user_id_scene_key</c> 保证一键一行
/// 写得频繁，标 <c>[DiffLogIgnored]</c> 不进数据变更日志
/// </remarks>
[SugarTable(TableName = "sys_user_setting", TableDescription = "用户设置，供前端跨端同步偏好、收藏与页面设置")]
[ActiveUniqueIndex(nameof(UserId), nameof(Scene), nameof(Key), Name = "ux_sys_user_setting_user_id_scene_key")]
[DiffLogIgnored]
public sealed class SysUserSetting : AppAuditedEntity
{
    /// <summary>
    /// 用户
    /// </summary>
    [SugarColumn(ColumnName = "user_id", IsNullable = false, ColumnDescription = "用户")]
    public Guid UserId { get; set; }

    /// <summary>
    /// 场景
    /// </summary>
    [SugarColumn(ColumnName = "scene", IsNullable = false, ColumnDescription = "场景")]
    public UserSettingScene Scene { get; set; } = UserSettingScene.Preference;

    /// <summary>
    /// 设置键：偏好场景是固定标识（如 <c>global</c>），页面场景是页面编码
    /// </summary>
    [SugarColumn(ColumnName = "key", Length = 512, IsNullable = false, ColumnDescription = "设置键：偏好场景是固定标识（如 global），页面场景是页面编码")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 设置载荷（JSON）
    /// </summary>
    [SugarColumn(ColumnName = "value", IsNullable = true, ColumnDescription = "设置载荷（JSON）")]
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
