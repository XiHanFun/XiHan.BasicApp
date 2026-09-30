// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.Framework.Data.SqlSugar.Routing;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统角色继承边实体：一行即一条「下级角色直接继承上级角色」
/// </summary>
/// <remarks>
/// 只存直接继承边，间接继承、深度与路径都由 RoleInheritanceGraph 从边推出。
/// 角色继承图很小、也没有 SQL 联表依赖展开结果，存派生的闭包只会带来一致性问题：
/// 平台调整全局角色的继承时，各租户里继承了它的角色随读取即时生效，不需要跨租户重建。
///
/// 作用范围：
/// - 租户角色可继承本租户角色与平台全局角色，边存本租户；全局角色只继承全局角色，边存平台，仅平台维护
/// - 系统角色（super_admin / tenant_owner）不参与继承：它们的权限由授权快照按上下文整体给出
///
/// 继承语义（详见 docs/backend/permission.md）：
/// - 有效继承链 = 角色自身 + 经启用角色可达的全部上级；停用角色不贡献权限，也切断经由它的继承
/// - 角色级 Deny 只作用于本链：链上 Grant 并集减去链上 Deny 并集，不影响用户持有的其他独立角色
/// - 数据范围、字段安全规则、成员上限不继承
/// - 职责分离按结构展开继承链判定（不看启停），变更继承时复核受影响角色与成员
///
/// 不设 Status/IsDeleted：解除继承即硬删这条边。
/// </remarks>
[SugarTable(TableName = "Sys_Role_Hierarchy", TableDescription = "系统角色继承关系表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_AnId_DeId", nameof(TenantId), OrderByType.Asc, nameof(AncestorId), OrderByType.Asc, nameof(DescendantId), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_DeId", nameof(DescendantId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_AnId", nameof(TenantId), OrderByType.Asc, nameof(AncestorId), OrderByType.Asc)]
[PlatformDataSource]
public partial class SysRoleHierarchy : BasicAppCreationEntity
{
    /// <summary>
    /// 上级角色ID（被继承的角色）
    /// </summary>
    [SugarColumn(ColumnName = "Ancestor_Id", ColumnDescription = "上级角色ID", IsNullable = false)]
    public virtual long AncestorId { get; set; }

    /// <summary>
    /// 下级角色ID（继承者角色）
    /// </summary>
    [SugarColumn(ColumnName = "Descendant_Id", ColumnDescription = "下级角色ID", IsNullable = false)]
    public virtual long DescendantId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "Remark", ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
