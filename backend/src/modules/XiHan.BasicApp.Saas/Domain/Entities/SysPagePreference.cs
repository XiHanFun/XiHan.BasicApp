#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysPagePreference
// Guid:5a1c7e92-3b4d-4f8a-9c1e-7d2b6f0a4e51
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/05 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统页面偏好实体
/// 按 (用户 × 页面码) 存储列表页个性化偏好（列设置/密度/个人视图等），JSON 载荷，跨端同步。
/// </summary>
/// <remarks>
/// 职责边界：
/// - 与前端 Schema 驱动列表页的 pageCode 一一对应；每个用户每页一条（UX_UsId_PaCo）
/// - 仅存"个人偏好载荷"（不可信前端结构），由前端按版本兼容解析；后端不解释 Payload 语义
///
/// 写入：
/// - upsert：按 (UserId, PageCode) 命中则更新 Payload，否则新建
///
/// 查询：
/// - 列表页加载时按 (UserId, PageCode) 读取，命中则覆盖 localStorage 默认
///
/// 场景：
/// - 列设置（显隐/顺序/固定/列宽）、密度、个人视图（筛选+排序+分页快照）跨端同步
/// </remarks>
[SugarTable("SysPagePreference", "系统页面偏好表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_UsId_PaCo", nameof(UserId), OrderByType.Asc, nameof(PageCode), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc, true)]
public partial class SysPagePreference : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "用户ID", IsNullable = false)]
    public virtual long UserId { get; set; }

    /// <summary>
    /// 页面唯一码（与前端 PageSchema.pageCode 对应）
    /// </summary>
    [SugarColumn(ColumnDescription = "页面码", Length = 100, IsNullable = false)]
    public virtual string PageCode { get; set; } = string.Empty;

    /// <summary>
    /// 偏好载荷（JSON：列显隐/顺序/固定/列宽/密度/个人视图等）
    /// </summary>
    [SugarColumn(ColumnDescription = "偏好载荷(JSON)", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? Payload { get; set; }
}
