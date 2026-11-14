// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Access.Domain.Entities;

/// <summary>
/// 角色继承的一条直接边（平台库实体，随所属侧）
/// </summary>
/// <remarks>
/// <see cref="RoleId"/> 继承 <see cref="ParentId"/> 的授予（方案 5.9），上级的拒绝同样作用于下级；只存直接上级的边，祖先在判定时按边广度优先展开，改一条边不必重算整条链
/// 停用的上级不传下授予，也截断经过它的继承：停用一个角色就切断它往下的授予，不必拆边
/// 只在同一侧的角色之间、内置角色不参与；写入时防环（新上级不能是本角色的下级）、防冗余（直接上级之间不能互为祖先），校验按本次工作单元读库里的边而不按缓存，缓存里可能还是旧图
/// 新加上级会让持有本角色及其下级的用户多持有角色，所以按静态职责分离重查，拦截类违规即整体不做
/// 继承来的授予不写进框架的授予存储，由 <c>AccessGrantSource</c> 在存储没命中时按继承图给出：上级的授予变了，下级不必跟着改写；一侧的整张图连同角色启停放共享缓存 60 秒，改边与角色启停后失效
/// 解除即物理删除；删角色时去掉它作为下级的边，它还是别的角色的上级时不能删
/// </remarks>
[SugarTable(TableName = "sys_role_inheritance", TableDescription = "角色继承的一条直接边（平台库实体，随所属侧）")]
[SugarIndex("ux_sys_role_inheritance_owner_tenant_id_role_id_parent_id", nameof(OwnerTenantId), OrderByType.Asc, nameof(RoleId), OrderByType.Asc, nameof(ParentId), OrderByType.Asc, true)]
public sealed class SysRoleInheritance : AppRecord, IOwnedByTenant
{
    /// <inheritdoc />
    [SugarColumn(ColumnName = "owner_tenant_id", Length = 512, IsNullable = false, ColumnDescription = "属主租户 ID")]
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 下级角色（继承者）
    /// </summary>
    [SugarColumn(ColumnName = "role_id", IsNullable = false, ColumnDescription = "下级角色（继承者）")]
    public Guid RoleId { get; set; }

    /// <summary>
    /// 直接上级角色
    /// </summary>
    [SugarColumn(ColumnName = "parent_id", IsNullable = false, ColumnDescription = "直接上级角色")]
    public Guid ParentId { get; set; }
}
