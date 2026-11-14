// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Access.Domain.Entities;

/// <summary>
/// 角色（平台库实体，不实现 <c>IMultiTenant</c>）
/// </summary>
/// <remarks>
/// 平台库实体、不实现 <c>IMultiTenant</c>：<see cref="OwnerTenantId"/> 为 <c>platform</c> 的是平台角色，否则是该租户的角色；平台与各租户的角色同表，按所属侧隔离，别的侧的角色按 ID 也找不到
/// 授予写在框架的 <c>PermissionGrantRecord</c>，主体是 <c>role:&lt;角色 ID&gt;</c>、租户列按所属侧：用 ID 不用角色码，各租户可以有同码的角色而授予不串
/// 角色码在同一侧唯一且创建后不可改，只作稳定的业务标识；是部分唯一索引，软删除后同一角色码可以重建。令牌里的角色声明与各关联表都按角色 ID 引用
/// 数据范围直接存在角色上，只有自定义范围才要部门清单，拆到 <see cref="SysRoleDataScope"/>；平台没有部门，平台角色只能是全部
/// 停用保留成员与授予，只是不进令牌、也不往下级传授予：可以临时冻结一个角色而不丢配置
/// 内建角色 <c>admin</c>（<see cref="IsBuiltIn"/>，授予 <c>*</c>）由平台种子与新租户初始化各建一个，不能删，也不参与继承、委托与权限条件
/// 删除是软删除，但成员指派、授予、规则行、条件、委托、字段安全规则、自定义部门与它作为下级的继承边一并清掉：不会留下仍在生效的授予；仍被别的角色继承的不能删，免得悄悄拿走下级持有人的授予
/// </remarks>
[SugarTable(TableName = "sys_role", TableDescription = "角色（平台库实体，不实现 IMultiTenant）")]
[ActiveUniqueIndex(nameof(OwnerTenantId), nameof(Code), Name = "ux_sys_role_owner_tenant_id_code")]
public sealed class SysRole : AppAuditedEntity, IOwnedByTenant
{
    /// <inheritdoc />
    [SugarColumn(ColumnName = "owner_tenant_id", Length = 512, IsNullable = false, ColumnDescription = "属主租户 ID")]
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 角色码：小写字母开头，只含小写字母、数字、<c>-</c>
    /// </summary>
    [SugarColumn(ColumnName = "code", Length = 512, IsNullable = false, ColumnDescription = "角色码：小写字母开头，只含小写字母、数字、-")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 显示名
    /// </summary>
    [SugarColumn(ColumnName = "name", Length = 512, IsNullable = false, ColumnDescription = "显示名")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 说明
    /// </summary>
    [SugarColumn(ColumnName = "description", Length = 512, IsNullable = true, ColumnDescription = "说明")]
    public string? Description { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    [SugarColumn(ColumnName = "order", IsNullable = false, ColumnDescription = "排序号")]
    public int Order { get; set; }

    /// <summary>
    /// 内建（种子写入）：不能删除
    /// </summary>
    [SugarColumn(ColumnName = "is_built_in", IsNullable = false, ColumnDescription = "内建（种子写入）：不能删除")]
    public bool IsBuiltIn { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "status", IsNullable = false, ColumnDescription = "状态")]
    public RoleStatus Status { get; set; } = RoleStatus.Active;

    /// <summary>
    /// 数据范围
    /// </summary>
    /// <remarks>
    /// 租户角色缺省只看自己，平台角色固定为全部；<see cref="Entities.DataScope.Custom"/> 的部门在 <see cref="SysRoleDataScope"/>
    /// </remarks>
    [SugarColumn(ColumnName = "data_scope", IsNullable = false, ColumnDescription = "数据范围")]
    public DataScope DataScope { get; set; } = DataScope.SelfOnly;
}

/// <summary>
/// 角色状态
/// </summary>
public enum RoleStatus
{
    /// <summary>
    /// 生效：成员的令牌带该角色
    /// </summary>
    Active = 0,

    /// <summary>
    /// 停用：保留成员与授予，但不进入令牌
    /// </summary>
    Disabled = 1,
}
