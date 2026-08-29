// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.Framework.Data.SqlSugar.Routing;

namespace XiHan.BasicApp.Sample.Domain.Entities;

/// <summary>
/// 示例 Erp 订单实体：经 <see cref="ModuleDataSourceAttribute"/> 声明落在 Erp 模块库。
/// </summary>
/// <remarks>
/// <para>
/// 只需在实体上标一次 <c>[ModuleDataSource("Erp")]</c>，该实体的仓储读写与建表就固定落在这个模块库上，
/// 业务代码不用切连接、不用自己拿 <c>ISqlSugarClient</c>。
/// </para>
/// <para>
/// 租户仍是统一的：模块库默认由所有租户共用，实体继承的 <c>TenantId</c> 行级过滤照常生效；
/// 某个租户要独占这个模块库时，在它的租户连接里补一条模块配置即可，实体一个字都不用改。
/// </para>
/// <para>
/// 模块数据源在当前布局与默认布局里都没有配置时，框架直接抛异常，不会静默回落到主库。
/// </para>
/// <para>
/// 同一工作单元内跨库写入时，每个连接各开一个本地事务，框架不提供跨库分布式事务。
/// </para>
/// </remarks>
[SugarTable(TableName = "Sample_Erp_Order", TableDescription = "示例 Erp 订单表")]
[ModuleDataSource(SampleModuleDataSources.Erp)]
public class SampleErpOrder : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 订单号
    /// </summary>
    [SugarColumn(ColumnName = "Order_No", ColumnDescription = "订单号", Length = 64, IsNullable = false)]
    public virtual string OrderNo { get; set; } = string.Empty;

    /// <summary>
    /// 订单金额
    /// </summary>
    [SugarColumn(ColumnName = "Amount", ColumnDescription = "订单金额", IsNullable = false)]
    public virtual decimal Amount { get; set; }
}
