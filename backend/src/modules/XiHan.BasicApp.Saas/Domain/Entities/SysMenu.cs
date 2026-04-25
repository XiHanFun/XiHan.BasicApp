#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysMenu
// Guid:5c28152c-d6e9-4396-addb-b479254bad0f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 02:40:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统菜单实体
/// 纯 UI 结构：负责前端导航/路由/展示；权限鉴权完全由其关联的 SysPermission 决定
/// </summary>
/// <remarks>
/// 职责边界：
/// - 菜单 ≠ 资源 ≠ 权限；菜单仅描述 UI 层级，通过 PermissionId 反向绑定一个权限点
/// - 后端鉴权永远基于 Permission，不依赖菜单存在性
///
/// 关联：
/// - PermissionId → SysPermission（可空；空表示纯展示菜单，无需鉴权）
/// - ParentId → SysMenu（自关联树结构）
///
/// 写入：
/// - TenantId + MenuCode 租户内唯一（UX_TeId_MeCo）
/// - IsGlobal=true 作为平台菜单模板，新租户初始化时克隆
/// - 树结构写入必须做环路检测（禁止 A→B→A）
///
/// 查询：
/// - 前端菜单树：按 TenantId + Status + PermissionId（可见）过滤，ORDER BY Sort
/// - 按 ParentId 构建层级：IX_PaId
/// - 按权限反查菜单：IX_PeId
///
/// 删除：
/// - 仅软删；删除父菜单前必须先处理子菜单（级联软删或提示）
///
/// 状态：
/// - Status: Yes/No 启停（停用菜单对所有用户隐藏）
/// - MenuType: 目录/菜单/按钮/外链等
///
/// 场景：
/// - 登录后按用户权限集合过滤可见菜单树
/// - 按钮级权限：MenuType=Button 配合 PermissionId 控制按钮显示
/// </remarks>
[SugarTable("SysMenu", "系统菜单表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_MeCo", nameof(TenantId), OrderByType.Asc, nameof(MenuCode), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_PaId", nameof(ParentId), OrderByType.Asc)]
[SugarIndex("IX_{table}_PeId", nameof(PermissionId), OrderByType.Asc)]
[SugarIndex("IX_{table}_MeTy", nameof(MenuType), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_St", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
[SugarIndex("IX_{table}_IsGl", nameof(IsGlobal), OrderByType.Asc)]
public partial class SysMenu : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 权限ID（菜单可见性所需的权限，为空表示纯展示菜单无需鉴权）
    /// </summary>
    /// <remarks>
    /// 设计决策：单一 PermissionId 是有意设计——菜单可见性由一个权限点控制。
    /// 若菜单需要多个权限才能访问（如同时需要 user:read 和 department:read），
    /// 应创建组合权限点（如 user-dept:view）绑定到菜单，而非引入多对多关联表。
    /// 这样保证后端鉴权永远基于 Permission，菜单只是 UI 层的展示映射。
    /// </remarks>
    [SugarColumn(ColumnDescription = "权限ID", IsNullable = true)]
    public virtual long? PermissionId { get; set; }

    /// <summary>
    /// 是否平台级全局菜单（全局菜单作为所有租户的基础模板，TenantId = 0）
    /// </summary>
    [SugarColumn(ColumnDescription = "是否全局菜单")]
    public virtual bool IsGlobal { get; set; } = false;

    /// <summary>
    /// 父级菜单ID
    /// </summary>
    [SugarColumn(ColumnDescription = "父级菜单ID", IsNullable = true)]
    public virtual long? ParentId { get; set; }

    /// <summary>
    /// 菜单名称
    /// </summary>
    [SugarColumn(ColumnDescription = "菜单名称", Length = 100, IsNullable = false)]
    public virtual string MenuName { get; set; } = string.Empty;

    /// <summary>
    /// 菜单编码（唯一标识）
    /// </summary>
    [SugarColumn(ColumnDescription = "菜单编码", Length = 100, IsNullable = false)]
    public virtual string MenuCode { get; set; } = string.Empty;

    /// <summary>
    /// 菜单类型
    /// </summary>
    [SugarColumn(ColumnDescription = "菜单类型")]
    public virtual MenuType MenuType { get; set; } = MenuType.Directory;

    /// <summary>
    /// 路由地址（前端路由路径）
    /// </summary>
    [SugarColumn(ColumnDescription = "路由地址", Length = 200, IsNullable = true)]
    public virtual string? Path { get; set; }

    /// <summary>
    /// 组件路径（前端组件文件路径）
    /// </summary>
    [SugarColumn(ColumnDescription = "组件路径", Length = 200, IsNullable = true)]
    public virtual string? Component { get; set; }

    /// <summary>
    /// 路由名称（用于 Vue Router 的 name 属性）
    /// </summary>
    [SugarColumn(ColumnDescription = "路由名称", Length = 100, IsNullable = true)]
    public virtual string? RouteName { get; set; }

    /// <summary>
    /// 重定向地址
    /// </summary>
    [SugarColumn(ColumnDescription = "重定向地址", Length = 200, IsNullable = true)]
    public virtual string? Redirect { get; set; }

    /// <summary>
    /// 菜单图标
    /// </summary>
    [SugarColumn(ColumnDescription = "菜单图标", Length = 100, IsNullable = true)]
    public virtual string? Icon { get; set; }

    /// <summary>
    /// 菜单标题（用于面包屑、标签页等）
    /// </summary>
    [SugarColumn(ColumnDescription = "菜单标题", Length = 100, IsNullable = true)]
    public virtual string? Title { get; set; }

    /// <summary>
    /// 是否外链
    /// </summary>
    [SugarColumn(ColumnDescription = "是否外链")]
    public virtual bool IsExternal { get; set; } = false;

    /// <summary>
    /// 外链地址（当 IsExternal=true 时使用）
    /// </summary>
    [SugarColumn(ColumnDescription = "外链地址", Length = 500, IsNullable = true)]
    public virtual string? ExternalUrl { get; set; }

    /// <summary>
    /// 是否缓存（Keep-Alive）
    /// </summary>
    [SugarColumn(ColumnDescription = "是否缓存")]
    public virtual bool IsCache { get; set; } = false;

    /// <summary>
    /// 是否显示在菜单中
    /// </summary>
    [SugarColumn(ColumnDescription = "是否显示")]
    public virtual bool IsVisible { get; set; } = true;

    /// <summary>
    /// 是否固定在标签页（不可关闭）
    /// </summary>
    [SugarColumn(ColumnDescription = "是否固定标签")]
    public virtual bool IsAffix { get; set; } = false;

    /// <summary>
    /// 标签内容（如 "New"、"3" 等，显示在侧栏菜单项右侧）
    /// </summary>
    [SugarColumn(ColumnDescription = "标签内容", Length = 50, IsNullable = true)]
    public virtual string? Badge { get; set; }

    /// <summary>
    /// 标签类型（控制标签颜色：default/success/warning/error/info）
    /// </summary>
    [SugarColumn(ColumnDescription = "标签类型", Length = 20, IsNullable = true)]
    public virtual string? BadgeType { get; set; }

    /// <summary>
    /// 是否仅显示标签圆点（为 true 时忽略标签内容，仅显示小圆点）
    /// </summary>
    [SugarColumn(ColumnDescription = "标签圆点")]
    public virtual bool BadgeDot { get; set; } = false;

    /// <summary>
    /// 菜单元数据（JSON格式，存储额外配置）
    /// 例如：{"activeMenu": "/system/user"}
    /// </summary>
    [SugarColumn(ColumnDescription = "菜单元数据", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? Metadata { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnDescription = "状态")]
    public virtual YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 排序
    /// </summary>
    [SugarColumn(ColumnDescription = "排序")]
    public virtual int Sort { get; set; } = 0;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
