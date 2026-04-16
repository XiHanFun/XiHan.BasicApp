#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysResource
// Guid:2a3b4c5d-6e7f-8901-bcde-f12345678901
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/07 10:10:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统资源实体
/// 权限体系中的"被控对象"（API/数据表/文件/报表等），扁平结构，与 UI 菜单完全解耦
/// </summary>
/// <remarks>
/// 职责边界：
/// - 本实体仅描述"保护对象"本身；"能对资源做什么"由 SysOperation 定义；"谁能做"由 SysPermission 组合
/// - 不包含菜单/UI 层级概念（UI 由 SysMenu 单独承载）
///
/// 关联：
/// - 反向：SysPermission.ResourceId、SysFieldLevelSecurity.ResourceId
///
/// 写入：
/// - TenantId + ResourceCode 租户内唯一（UX_TeId_ReCo）
/// - IsGlobal=true 时作为平台资源模板（TenantId 空），所有租户共享
/// - ResourcePath 对 API 类资源建议填写路由模式（便于自动扫描注册）
/// - Metadata JSON 用于扩展（如 controller/action、字段结构描述）
///
/// 查询：
/// - 按资源类型+状态筛选走复合索引 IX_TeId_ReTy_St
/// - 全局+私有合并查询：WHERE TenantId = ? OR IsGlobal = 1
///
/// 删除：
/// - 仅软删；删除前必须校验：无权限引用（SysPermission.ResourceId）、无字段级策略引用（SysFieldLevelSecurity.ResourceId）
///
/// 状态与访问级别：
/// - Status: Yes/No 启停
/// - AccessLevel: 替代原 IsRequireAuth+IsPublic 组合，明确表达匿名/认证/授权访问需求
///
/// 场景：
/// - 接口自动扫描注册：程序启动时将控制器 Action 注册为 API 类资源
/// - 动态资源：管理端定义自定义资源类型（报表/数据表/业务对象等）
/// </remarks>
[SugarTable("SysResource", "系统资源表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_ReCo", nameof(TenantId), OrderByType.Asc, nameof(ResourceCode), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_ReTy", nameof(ResourceType), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_ReTy_St", nameof(TenantId), OrderByType.Asc, nameof(ResourceType), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
[SugarIndex("IX_{table}_IsGl", nameof(IsGlobal), OrderByType.Asc)]
public partial class SysResource : BasicAppAggregateRoot
{
    /// <summary>
    /// 资源编码（唯一标识，如：user, order, department）
    /// </summary>
    [SugarColumn(ColumnDescription = "资源编码", Length = 100, IsNullable = false)]
    public virtual string ResourceCode { get; set; } = string.Empty;

    /// <summary>
    /// 资源名称
    /// </summary>
    [SugarColumn(ColumnDescription = "资源名称", Length = 100, IsNullable = false)]
    public virtual string ResourceName { get; set; } = string.Empty;

    /// <summary>
    /// 资源类型
    /// </summary>
    [SugarColumn(ColumnDescription = "资源类型")]
    public virtual ResourceType ResourceType { get; set; } = ResourceType.Api;

    /// <summary>
    /// 资源路径（URL/文件路径/API路径等）
    /// </summary>
    [SugarColumn(ColumnDescription = "资源路径", Length = 500, IsNullable = true)]
    public virtual string? ResourcePath { get; set; }

    /// <summary>
    /// 资源描述
    /// </summary>
    [SugarColumn(ColumnDescription = "资源描述", Length = 500, IsNullable = true)]
    public virtual string? Description { get; set; }

    /// <summary>
    /// 资源元数据（JSON格式，存储扩展信息）
    /// 例如：{"method": "GET", "controller": "UserController", "action": "GetList"}
    /// </summary>
    [SugarColumn(ColumnDescription = "资源元数据", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? Metadata { get; set; }

    /// <summary>
    /// 资源访问级别（替代原 IsRequireAuth+IsPublic，消除无效布尔组合）
    /// </summary>
    [SugarColumn(ColumnDescription = "访问级别")]
    public virtual ResourceAccessLevel AccessLevel { get; set; } = ResourceAccessLevel.Authorized;

    /// <summary>
    /// 是否平台级全局资源（全局资源所有租户共享，TenantId 为空）
    /// </summary>
    [SugarColumn(ColumnDescription = "是否全局资源")]
    public virtual bool IsGlobal { get; set; } = false;

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
