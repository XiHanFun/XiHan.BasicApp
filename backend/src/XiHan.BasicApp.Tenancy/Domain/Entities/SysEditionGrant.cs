// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Tenancy.Domain.Entities;

/// <summary>
/// 套餐白名单的一行（平台库，表 <c>sys_edition_grant</c>）
/// </summary>
/// <remarks>
/// 一个权限码一行，普通唯一索引 <c>ux_sys_edition_grant_edition_id_permission_code</c> 约束同一套餐里不重复；只能是租户能生效的权限（作用侧 Tenant / Both）
/// 基类是只增记录 <c>AppRecord</c>、物理删除：白名单只表达现状，改白名单（<c>editions.set-permissions</c>）时另记一条权限变更日志留痕；整体替换时多的删、少的补，没变的行不动
/// 在每次权限判定的热路径上：门控按租户把整份白名单读进共享缓存（30 秒），改白名单后主动失效绑定这个套餐的租户，跨实例靠存活时间收敛
/// 之后新增的权限不会自动进已有套餐（种子按编码只首写），由运营在套餐页授予
/// </remarks>
[SugarTable(TableName = "sys_edition_grant", TableDescription = "套餐白名单的一行（平台库，表 sys_edition_grant）")]
[SugarIndex("ux_sys_edition_grant_edition_id_permission_code", nameof(EditionId), OrderByType.Asc, nameof(PermissionCode), OrderByType.Asc, true)]
public sealed class SysEditionGrant : AppRecord
{
    /// <summary>
    /// 套餐主键
    /// </summary>
    [SugarColumn(ColumnName = "edition_id", IsNullable = false, ColumnDescription = "套餐主键")]
    public Guid EditionId { get; set; }

    /// <summary>
    /// 权限码
    /// </summary>
    [SugarColumn(ColumnName = "permission_code", Length = 512, IsNullable = false, ColumnDescription = "权限码")]
    public string PermissionCode { get; set; } = string.Empty;
}
