#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserSetting
// Guid:5a1c7e92-3b4d-4f8a-9c1e-7d2b6f0a4e51
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/10 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统用户设置实体
/// 按 (用户 × 场景 × 设置键) 存储个性化设置的 JSON 载荷，覆盖全场景（偏好设置 / 页面设置 …），跨端同步。
/// </summary>
/// <remarks>
/// 职责边界：
/// - 通用「用户级设置同步」存储，不局限于页面偏好；场景由 <see cref="UserSettingScene"/> 区分
/// - 仅存"个人设置载荷"（不可信前端结构），由前端按版本兼容解析；后端不解释 SettingValue 语义
///
/// 写入：
/// - upsert：按 (UserId, Scene, SettingKey) 命中则更新 SettingValue，否则新建
///
/// 查询：
/// - 按 (UserId, Scene, SettingKey) 读取，命中则覆盖 localStorage 默认；读侧带分布式缓存
///
/// 场景：
/// - 偏好设置（Preference）：主题/外观/布局/组件/快捷键，设置键固定（如 "global"）
/// - 页面设置（Page）：列设置（显隐/顺序/固定/列宽）、密度、个人视图（筛选+排序+分页快照），设置键为 pageCode
/// </remarks>
[SugarTable("SysUserSetting", "系统用户设置表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_UsId_Sc_SeKe", nameof(UserId), OrderByType.Asc, nameof(Scene), OrderByType.Asc, nameof(SettingKey), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc, true)]
public partial class SysUserSetting : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "用户ID", IsNullable = false)]
    public virtual long UserId { get; set; }

    /// <summary>
    /// 设置场景（偏好设置 / 页面设置 …）
    /// </summary>
    [SugarColumn(ColumnDescription = "设置场景", IsNullable = false)]
    public virtual UserSettingScene Scene { get; set; } = UserSettingScene.Preference;

    /// <summary>
    /// 设置键（偏好设置场景为固定标识；页面设置场景为 pageCode）
    /// </summary>
    [SugarColumn(ColumnDescription = "设置键", Length = 100, IsNullable = false)]
    public virtual string SettingKey { get; set; } = string.Empty;

    /// <summary>
    /// 设置载荷（JSON；后端不解释语义，由前端按版本兼容解析）
    /// </summary>
    [SugarColumn(ColumnDescription = "设置载荷(JSON)", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? SettingValue { get; set; }
}
