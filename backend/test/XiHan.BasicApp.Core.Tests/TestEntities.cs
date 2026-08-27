// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.Framework.Domain.Events;
using XiHan.Framework.Domain.Events.Abstracts;

namespace XiHan.BasicApp.Core.Tests;

/// <summary>
/// 测试专用样例实体集合。
/// </summary>
/// <remarks>
/// 七个 BasicApp 实体基类都是 <c>abstract</c> 且构造函数为 <c>protected</c>，无法直接实例化。
/// 这里为每个基类派生一个最小样例，只做两件事：把 protected 构造函数提升为 public 供测试调用，
/// 以及为 CodeFirst 建表提供带 <c>[SugarTable]</c>/<c>[SugarIndex]</c> 的落库形状。
/// <para>
/// 刻意不引用 src 下的任何生产实体：生产实体随业务演进增删字段，拿它做基类形状断言会把
/// 「基类契约变了」和「某张业务表加了字段」混为一谈。样例实体只声明一个 <c>Code</c> 列，
/// 除此之外的每一列都来自基类，断言失败必定指向基类。
/// </para>
/// </remarks>
internal static class TestEntities
{
    /// <summary>
    /// 完整审计样例实体的表名。
    /// </summary>
    internal const string FullAuditedTableName = "Core_Test_Full_Audited";

    /// <summary>
    /// 创建审计样例实体的表名。
    /// </summary>
    internal const string CreationTableName = "Core_Test_Creation";

    /// <summary>
    /// 聚合根样例实体的表名。
    /// </summary>
    internal const string AggregateRootTableName = "Core_Test_Aggregate_Root";
}

/// <summary>
/// 派生自 <see cref="BasicAppEntity"/> 的样例实体（无审计列、非自增主键）。
/// </summary>
public sealed class CoreEntityProbe : BasicAppEntity
{
    /// <summary>
    /// 构造函数（对应基类的 protected 无参构造）。
    /// </summary>
    public CoreEntityProbe()
    {
    }

    /// <summary>
    /// 构造函数（对应基类接收主键的 protected 构造）。
    /// </summary>
    /// <param name="basicId">主键。</param>
    public CoreEntityProbe(long basicId)
        : base(basicId)
    {
    }
}

/// <summary>
/// 另一个派生自 <see cref="BasicAppEntity"/> 的样例实体。
/// </summary>
/// <remarks>
/// 专供相等性测试：同一个 BasicId 但类型不同的两个实体必须不相等，
/// 否则缓存/集合去重会把不同表的同 Id 记录当成同一条。
/// </remarks>
public sealed class CoreOtherEntityProbe : BasicAppEntity
{
    /// <summary>
    /// 构造函数（对应基类的 protected 无参构造）。
    /// </summary>
    public CoreOtherEntityProbe()
    {
    }

    /// <summary>
    /// 构造函数（对应基类接收主键的 protected 构造）。
    /// </summary>
    /// <param name="basicId">主键。</param>
    public CoreOtherEntityProbe(long basicId)
        : base(basicId)
    {
    }
}

/// <summary>
/// 派生自 <see cref="BasicAppEntityWithIdentity"/> 的样例实体（自增主键）。
/// </summary>
public sealed class CoreEntityWithIdentityProbe : BasicAppEntityWithIdentity
{
}

/// <summary>
/// 派生自 <see cref="BasicAppCreationEntity"/> 的样例实体（只有创建审计，硬删）。
/// </summary>
[SugarTable(TableName = TestEntities.CreationTableName, TableDescription = "核心库创建审计样例表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
public sealed class CoreCreationProbe : BasicAppCreationEntity
{
    /// <summary>
    /// 业务编码。
    /// </summary>
    [SugarColumn(ColumnName = "Code", ColumnDescription = "业务编码", Length = 64, IsNullable = false)]
    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// 派生自 <see cref="BasicAppModificationEntity"/> 的样例实体（只有修改审计）。
/// </summary>
public sealed class CoreModificationProbe : BasicAppModificationEntity
{
}

/// <summary>
/// 派生自 <see cref="BasicAppDeletionEntity"/> 的样例实体（只有删除审计）。
/// </summary>
public sealed class CoreDeletionProbe : BasicAppDeletionEntity
{
}

/// <summary>
/// 派生自 <see cref="BasicAppFullAuditedEntity"/> 的样例实体（完整审计 + 软删唯一索引）。
/// </summary>
/// <remarks>
/// 唯一索引末列附加 <c>IsDeleted</c>，正是 BasicAppEntity 注释里写的软删唯一索引约定；
/// CodeFirst 测试靠它验证「软删后可重建同编码」与「同编码至多保留一条软删行」两条实际行为。
/// </remarks>
[SugarTable(TableName = TestEntities.FullAuditedTableName, TableDescription = "核心库完整审计样例表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_Cd", nameof(TenantId), OrderByType.Asc, nameof(Code), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc, true)]
public sealed class CoreFullAuditedProbe : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 构造函数（对应基类的 protected 无参构造）。
    /// </summary>
    public CoreFullAuditedProbe()
    {
    }

    /// <summary>
    /// 构造函数（对应基类接收主键的 protected 构造）。
    /// </summary>
    /// <param name="basicId">主键。</param>
    public CoreFullAuditedProbe(long basicId)
        : base(basicId)
    {
    }

    /// <summary>
    /// 业务编码。
    /// </summary>
    [SugarColumn(ColumnName = "Code", ColumnDescription = "业务编码", Length = 64, IsNullable = false)]
    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// 派生自 <see cref="BasicAppAggregateRoot"/> 的样例聚合根。
/// </summary>
public class CoreAggregateRootProbe : BasicAppAggregateRoot
{
    /// <summary>
    /// 构造函数（对应基类的 protected 无参构造）。
    /// </summary>
    public CoreAggregateRootProbe()
    {
    }

    /// <summary>
    /// 构造函数（对应基类接收主键的 protected 构造）。
    /// </summary>
    /// <param name="basicId">主键。</param>
    public CoreAggregateRootProbe(long basicId)
        : base(basicId)
    {
    }

    /// <summary>
    /// 把受保护的本地事件登记口暴露给测试。
    /// </summary>
    /// <param name="eventData">领域事件。</param>
    public void RaiseLocalEvent(IDomainEvent eventData)
    {
        AddLocalEvent(eventData);
    }

    /// <summary>
    /// 把受保护的分布式事件登记口暴露给测试。
    /// </summary>
    /// <param name="eventData">领域事件。</param>
    public void RaiseDistributedEvent(IDomainEvent eventData)
    {
        AddDistributedEvent(eventData);
    }
}

/// <summary>
/// 带落库形状的聚合根样例实体，供 CodeFirst 列名对比使用。
/// </summary>
[SugarTable(TableName = TestEntities.AggregateRootTableName, TableDescription = "核心库聚合根样例表")]
public sealed class CoreAggregateRootTableProbe : BasicAppAggregateRoot
{
    /// <summary>
    /// 业务编码。
    /// </summary>
    [SugarColumn(ColumnName = "Code", ColumnDescription = "业务编码", Length = 64, IsNullable = false)]
    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// 测试专用领域事件。
/// </summary>
public sealed class CoreProbeDomainEvent : DomainEventBase
{
}
