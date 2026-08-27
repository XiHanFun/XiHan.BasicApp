// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Entities;
using XiHan.Framework.Domain.Entities.Abstracts;

namespace XiHan.BasicApp.Core.Tests;

/// <summary>
/// BasicApp 实体基类家族的多租户语义测试。
/// </summary>
/// <remarks>
/// 这里守的是整个仓库最贵的一条约定：租户行过滤是经
/// <c>AddTableFilter&lt;IMultiTenantEntity&gt;</c> 注册的，SqlSugar 只对**可赋值给该接口**的实体套用。
/// 也就是说，一个实体即便声明了 TenantId 列，只要接口丢了，租户过滤就静默失效 —— 不报错、不告警，
/// 直接把别的租户的数据读出来。框架源码的注释里专门写了这个坑，本文件把它变成会红的断言。
/// <para>
/// 另一半是"读共享"口径：本家族**不得**实现 <see cref="IStrictMultiTenantEntity"/>。
/// 严格接口会把读口径收紧成租户与平台互不可见，字典、消息模板一类平台维护的共享数据
/// 会在租户侧突然全部消失。
/// </para>
/// </remarks>
public sealed class BasicAppEntityTenantSemanticsTests
{
    /// <summary>
    /// 实体基类家族的全部七个成员。
    /// </summary>
    public static TheoryData<Type> AllEntityBaseTypes =>
    [
        typeof(BasicAppEntity),
        typeof(BasicAppEntityWithIdentity),
        typeof(BasicAppCreationEntity),
        typeof(BasicAppModificationEntity),
        typeof(BasicAppDeletionEntity),
        typeof(BasicAppFullAuditedEntity),
        typeof(BasicAppAggregateRoot)
    ];

    /// <summary>
    /// 七个基类必须全部实现 <see cref="IMultiTenantEntity"/>，这是租户过滤生效的唯一前提。
    /// </summary>
    /// <param name="baseType">被检查的实体基类。</param>
    [Theory]
    [MemberData(nameof(AllEntityBaseTypes))]
    public void EntityBaseTypes_ShouldImplementMultiTenantEntity(Type baseType)
    {
        Assert.True(
            baseType.IsAssignableTo(typeof(IMultiTenantEntity)),
            $"{baseType.Name} 未实现 IMultiTenantEntity，租户行过滤会对其静默失效。");
    }

    /// <summary>
    /// 七个基类都不得实现 <see cref="IStrictMultiTenantEntity"/>，读口径必须保持"读共享"。
    /// </summary>
    /// <remarks>
    /// 严格隔离要在具体实体上按需标注，而不是从基类一刀切下发；一旦加到基类，
    /// 所有派生实体的平台级共享数据都会在租户态消失。
    /// </remarks>
    /// <param name="baseType">被检查的实体基类。</param>
    [Theory]
    [MemberData(nameof(AllEntityBaseTypes))]
    public void EntityBaseTypes_ShouldNotImplementStrictMultiTenantEntity(Type baseType)
    {
        Assert.False(
            baseType.IsAssignableTo(typeof(IStrictMultiTenantEntity)),
            $"{baseType.Name} 实现了 IStrictMultiTenantEntity，会让平台共享数据在租户态整体消失。");
    }

    /// <summary>
    /// TenantId 必须是非可空 long 且公开可读写。
    /// </summary>
    /// <remarks>
    /// 改成 <c>long?</c> 会让 <c>UNIQUE(TenantId, XxCode)</c> 这类复合唯一索引对平台记录失效
    /// —— NULL 在 MySQL / PostgreSQL 的唯一约束中互不相等，同一编码可被重复插入。
    /// </remarks>
    /// <param name="baseType">被检查的实体基类。</param>
    [Theory]
    [MemberData(nameof(AllEntityBaseTypes))]
    public void TenantId_ShouldBeNonNullableInt64(Type baseType)
    {
        var property = CoreTestHelper.RequireProperty(baseType, "TenantId");

        Assert.Equal(typeof(long), property.PropertyType);
        Assert.Null(Nullable.GetUnderlyingType(property.PropertyType));
        Assert.True(property.GetGetMethod()!.IsPublic);
        Assert.True(property.GetSetMethod()!.IsPublic);
        Assert.True(property.GetGetMethod()!.IsVirtual, "TenantId 必须是 virtual，派生实体才能改写列描述。");
    }

    /// <summary>
    /// 新建实体的 TenantId 默认必须是 0，即"平台/全局记录"。
    /// </summary>
    /// <remarks>
    /// 项目口径是「平台级记录统一用 TenantId = 0，不得使用 NULL」；
    /// 默认值一旦漂成别的数字，未显式赋租户的记录会落到某个真实租户名下。
    /// </remarks>
    [Fact]
    public void TenantId_ShouldDefaultToPlatformTenantZero()
    {
        Assert.Equal(0L, new CoreEntityProbe().TenantId);
        Assert.Equal(0L, new CoreEntityWithIdentityProbe().TenantId);
        Assert.Equal(0L, new CoreCreationProbe().TenantId);
        Assert.Equal(0L, new CoreModificationProbe().TenantId);
        Assert.Equal(0L, new CoreDeletionProbe().TenantId);
        Assert.Equal(0L, new CoreFullAuditedProbe().TenantId);
        Assert.Equal(0L, new CoreAggregateRootProbe().TenantId);
    }

    /// <summary>
    /// 通过接口读写 TenantId 必须与实体属性是同一份状态。
    /// </summary>
    /// <remarks>
    /// 租户上下文是按接口给实体注入租户号的；若接口成员被显式实现成另一份存储，
    /// 注入进去的租户号不会落到实际列上。
    /// </remarks>
    [Fact]
    public void TenantId_InterfaceAccessShouldShareEntityState()
    {
        var entity = new CoreFullAuditedProbe();
        IMultiTenantEntity view = entity;

        view.TenantId = 7L;

        Assert.Equal(7L, entity.TenantId);

        entity.TenantId = 9L;

        Assert.Equal(9L, view.TenantId);
    }

    /// <summary>
    /// 实体家族的 TenantId 列名必须是 Tenant_Id，且带 IsOnlyIgnoreUpdate。
    /// </summary>
    /// <remarks>
    /// <c>IsOnlyIgnoreUpdate</c> 丢失后 UPDATE 会带上 Tenant_Id，一次误更新即可把记录搬到别的租户名下，
    /// 属于跨租户写越权；列名改动则等价于线上列改名，全部手写升级脚本立即失效。
    /// </remarks>
    /// <param name="baseType">被检查的实体基类。</param>
    [Theory]
    [InlineData(typeof(BasicAppEntity))]
    [InlineData(typeof(BasicAppEntityWithIdentity))]
    [InlineData(typeof(BasicAppCreationEntity))]
    [InlineData(typeof(BasicAppModificationEntity))]
    [InlineData(typeof(BasicAppDeletionEntity))]
    [InlineData(typeof(BasicAppFullAuditedEntity))]
    public void TenantId_EntityFamilyShouldMapToSnakeCaseColumn(Type baseType)
    {
        var column = CoreTestHelper.RequireSugarColumn(baseType, "TenantId");

        Assert.Equal("Tenant_Id", column.ColumnName, StringComparer.Ordinal);
        Assert.True(column.IsOnlyIgnoreUpdate, "Tenant_Id 必须在 UPDATE 中被忽略，否则可跨租户改写归属。");
    }

    /// <summary>
    /// 聚合根家族的 TenantId 目前**没有**指定列名 —— 锁定这一实际差异。
    /// </summary>
    /// <remarks>
    /// 框架 <c>SugarMultiTenantAggregateRoot</c> 的 TenantId 只写了描述与 IsOnlyIgnoreUpdate，
    /// 没有 ColumnName。于是 CodeFirst 会按属性名建列（PostgreSQL 下未加引号标识符折叠为小写 <c>tenantid</c>），
    /// 与实体家族的 <c>tenant_id</c> 形成两套命名并存。
    /// <para>
    /// 这条断言的用途不是"认为这样是对的"，而是当有人顺手补上 ColumnName 时立刻变红，
    /// 提醒必须同时给已上线的聚合根表出列重命名升级脚本，否则线上直接列不存在。
    /// </para>
    /// </remarks>
    [Fact]
    public void TenantId_AggregateRootFamilyStillHasNoExplicitColumnName()
    {
        var column = CoreTestHelper.RequireSugarColumn(typeof(BasicAppAggregateRoot), "TenantId");

        Assert.True(
            string.IsNullOrEmpty(column.ColumnName),
            "聚合根 TenantId 补了 ColumnName：这是列改名，必须配套升级脚本后再更新本断言。");
        Assert.True(column.IsOnlyIgnoreUpdate);
    }

    /// <summary>
    /// TenantId 可写，但语义上只在插入时生效，实体自身不做任何守卫。
    /// </summary>
    /// <remarks>
    /// 说明性断言：基类不拦截业务代码写 TenantId，跨租户防护完全依赖服务层的
    /// ITenantContext 与写路径租户守卫。把这条写出来，是为了避免有人误以为"实体层已经防住了"。
    /// </remarks>
    [Fact]
    public void TenantId_ShouldBeWritableWithoutEntityLevelGuard()
    {
        var entity = new CoreFullAuditedProbe
        {
            TenantId = 12345L
        };

        Assert.Equal(12345L, entity.TenantId);
    }

    /// <summary>
    /// 七个基类都不得出现落库的 IsGlobal 列。
    /// </summary>
    /// <remarks>
    /// 源码注释写明：全局记录一律以 <c>TenantId == 0</c> 判定，IsGlobal 只能是派生只读属性、不再落库。
    /// 一旦基类上冒出 IsGlobal 属性，它与 TenantId 就会各自漂移，出现"标了全局但租户号非 0"的行。
    /// </remarks>
    /// <param name="baseType">被检查的实体基类。</param>
    [Theory]
    [MemberData(nameof(AllEntityBaseTypes))]
    public void EntityBaseTypes_ShouldNotDeclareIsGlobalProperty(Type baseType)
    {
        Assert.Null(CoreTestHelper.FindProperty(baseType, "IsGlobal"));
    }
}
