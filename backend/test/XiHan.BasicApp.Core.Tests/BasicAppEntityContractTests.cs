// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using XiHan.BasicApp.Core.Entities;
using XiHan.Framework.Data.SqlSugar.Aggregates;
using XiHan.Framework.Data.SqlSugar.Entities;
using XiHan.Framework.Domain.Aggregates;
using XiHan.Framework.Domain.Aggregates.Abstracts;
using XiHan.Framework.Domain.Entities;

namespace XiHan.BasicApp.Core.Tests;

/// <summary>
/// BasicApp 实体基类家族的类型形状与相等性语义测试。
/// </summary>
/// <remarks>
/// 这七个基类被全仓库上千个实体继承，是"地基"：它们的抽象性、构造函数可见性、继承链
/// 一旦被改动，落库列集合与仓储的新建/更新判定会静默改变，而编译期不会报任何错。
/// 本文件锁定的是类型层面的形状；列特性与审计字段分别由
/// <see cref="BasicAppEntitySugarColumnTests"/> 与 <see cref="BasicAppEntityAuditShapeTests"/> 覆盖。
/// </remarks>
public sealed class BasicAppEntityContractTests
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
    /// 七个基类必须全部是 public abstract 的非泛型类。
    /// </summary>
    /// <remarks>
    /// 一旦某个基类被改成可实例化的具体类，SqlSugar CodeFirst 会把基类本身也当作一张表来建，
    /// 产生无业务含义的脏表；改成 internal 则模块侧的实体无法继承，整个仓库编译中断。
    /// </remarks>
    /// <param name="baseType">被检查的实体基类。</param>
    [Theory]
    [MemberData(nameof(AllEntityBaseTypes))]
    public void EntityBaseTypes_ShouldBePublicAbstractNonGenericClasses(Type baseType)
    {
        Assert.True(baseType.IsClass, $"{baseType.Name} 必须是类。");
        Assert.True(baseType.IsAbstract, $"{baseType.Name} 必须是抽象类，否则 CodeFirst 会为基类建表。");
        Assert.True(baseType.IsPublic, $"{baseType.Name} 必须是 public，模块侧实体才能继承。");
        Assert.False(baseType.IsSealed && !baseType.IsAbstract, $"{baseType.Name} 不得为 sealed。");
        Assert.False(baseType.IsGenericType, $"{baseType.Name} 必须是非泛型的 long 主键特化。");
    }

    /// <summary>
    /// 七个基类的构造函数必须全部是 protected，禁止出现 public 构造。
    /// </summary>
    /// <remarks>
    /// 抽象类的构造函数只应由派生实体调用；出现 public 构造意味着有人打算直接 new 基类，
    /// 这在多租户/审计语义下是没有意义的对象。
    /// </remarks>
    /// <param name="baseType">被检查的实体基类。</param>
    [Theory]
    [MemberData(nameof(AllEntityBaseTypes))]
    public void EntityBaseTypes_ShouldOnlyExposeProtectedConstructors(Type baseType)
    {
        Assert.Empty(baseType.GetConstructors(BindingFlags.Instance | BindingFlags.Public));

        var constructors = baseType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotEmpty(constructors);
        Assert.All(constructors, ctor => Assert.True(ctor.IsFamily, $"{baseType.Name} 的构造函数必须是 protected。"));
    }

    /// <summary>
    /// 只有 Entity / FullAudited / AggregateRoot 三个基类再暴露"接收主键"的构造函数。
    /// </summary>
    /// <remarks>
    /// 这是本家族一条容易被忽略的实际形状：<see cref="BasicAppCreationEntity"/>、
    /// <see cref="BasicAppModificationEntity"/>、<see cref="BasicAppDeletionEntity"/>、
    /// <see cref="BasicAppEntityWithIdentity"/> 自身没有声明任何构造函数，因此只有编译器生成的
    /// protected 无参构造 —— 框架基类上那些接收 basicId / createdId 的重载**没有**被向下透出。
    /// 派生实体想在构造期指定主键只能靠 protected setter 自行赋值。
    /// 把这条锁死是为了：若哪天有人补齐重载，测试变红并促使显式确认（新增重载会改变
    /// 「主键只能由分布式 Id 生成器写入」的既有口径）。
    /// </remarks>
    /// <param name="baseType">被检查的实体基类。</param>
    /// <param name="shouldExposeBasicIdConstructor">该基类是否应当暴露接收主键的构造函数。</param>
    [Theory]
    [InlineData(typeof(BasicAppEntity), true)]
    [InlineData(typeof(BasicAppFullAuditedEntity), true)]
    [InlineData(typeof(BasicAppAggregateRoot), true)]
    [InlineData(typeof(BasicAppEntityWithIdentity), false)]
    [InlineData(typeof(BasicAppCreationEntity), false)]
    [InlineData(typeof(BasicAppModificationEntity), false)]
    [InlineData(typeof(BasicAppDeletionEntity), false)]
    public void EntityBaseTypes_ShouldExposeExpectedConstructorSignatures(Type baseType, bool shouldExposeBasicIdConstructor)
    {
        var signatures = baseType
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Select(ctor => string.Join(",", ctor.GetParameters().Select(parameter => parameter.ParameterType.Name)))
            .OrderBy(signature => signature, StringComparer.Ordinal)
            .ToList();

        if (shouldExposeBasicIdConstructor)
        {
            Assert.Equal(["", "Int64"], signatures);
        }
        else
        {
            Assert.Equal([""], signatures);
        }
    }

    /// <summary>
    /// 自增主键基类不得提供接收主键的构造函数。
    /// </summary>
    /// <remarks>
    /// 自增主键由数据库产生，允许外部在构造期指定主键会与 <c>IsIdentity = true</c> 的语义直接打架。
    /// </remarks>
    [Fact]
    public void BasicAppEntityWithIdentity_ShouldNotAcceptExternalBasicId()
    {
        var constructors = typeof(BasicAppEntityWithIdentity)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.All(constructors, ctor => Assert.Empty(ctor.GetParameters()));
    }

    /// <summary>
    /// BasicAppEntity 的继承链必须逐层精确，换任一层基类都会静默改变落库列集合。
    /// </summary>
    [Fact]
    public void BasicAppEntity_ShouldHaveExactInheritanceChain()
    {
        Assert.Equal(
            [
                typeof(BasicAppEntity),
                typeof(SugarMultiTenantEntity<long>),
                typeof(SugarEntity<long>),
                typeof(EntityBase<long>),
                typeof(EntityBase)
            ],
            CoreTestHelper.GetInheritanceChain(typeof(BasicAppEntity)));
    }

    /// <summary>
    /// 自增主键实体基类的继承链必须走 SugarEntityWithIdentity 一支。
    /// </summary>
    [Fact]
    public void BasicAppEntityWithIdentity_ShouldHaveExactInheritanceChain()
    {
        Assert.Equal(
            [
                typeof(BasicAppEntityWithIdentity),
                typeof(SugarMultiTenantEntityWithIdentity<long>),
                typeof(SugarEntityWithIdentity<long>),
                typeof(EntityBase<long>),
                typeof(EntityBase)
            ],
            CoreTestHelper.GetInheritanceChain(typeof(BasicAppEntityWithIdentity)));
    }

    /// <summary>
    /// 创建审计实体基类的继承链必须走 CreationEntityBase 一支（不得混入软删/修改基类）。
    /// </summary>
    [Fact]
    public void BasicAppCreationEntity_ShouldHaveExactInheritanceChain()
    {
        Assert.Equal(
            [
                typeof(BasicAppCreationEntity),
                typeof(SugarMultiTenantCreationEntity<long>),
                typeof(SugarCreationEntity<long>),
                typeof(CreationEntityBase<long>),
                typeof(EntityBase<long>),
                typeof(EntityBase)
            ],
            CoreTestHelper.GetInheritanceChain(typeof(BasicAppCreationEntity)));
    }

    /// <summary>
    /// 修改审计实体基类的继承链必须走 ModificationEntityBase 一支。
    /// </summary>
    [Fact]
    public void BasicAppModificationEntity_ShouldHaveExactInheritanceChain()
    {
        Assert.Equal(
            [
                typeof(BasicAppModificationEntity),
                typeof(SugarMultiTenantModificationEntity<long>),
                typeof(SugarModificationEntity<long>),
                typeof(ModificationEntityBase<long>),
                typeof(EntityBase<long>),
                typeof(EntityBase)
            ],
            CoreTestHelper.GetInheritanceChain(typeof(BasicAppModificationEntity)));
    }

    /// <summary>
    /// 删除审计实体基类的继承链必须走 DeletionEntityBase 一支。
    /// </summary>
    [Fact]
    public void BasicAppDeletionEntity_ShouldHaveExactInheritanceChain()
    {
        Assert.Equal(
            [
                typeof(BasicAppDeletionEntity),
                typeof(SugarMultiTenantDeletionEntity<long>),
                typeof(SugarDeletionEntity<long>),
                typeof(DeletionEntityBase<long>),
                typeof(EntityBase<long>),
                typeof(EntityBase)
            ],
            CoreTestHelper.GetInheritanceChain(typeof(BasicAppDeletionEntity)));
    }

    /// <summary>
    /// 完整审计实体基类的继承链必须走 FullAuditedEntityBase 一支。
    /// </summary>
    [Fact]
    public void BasicAppFullAuditedEntity_ShouldHaveExactInheritanceChain()
    {
        Assert.Equal(
            [
                typeof(BasicAppFullAuditedEntity),
                typeof(SugarMultiTenantFullAuditedEntity<long>),
                typeof(SugarFullAuditedEntity<long>),
                typeof(FullAuditedEntityBase<long>),
                typeof(EntityBase<long>),
                typeof(EntityBase)
            ],
            CoreTestHelper.GetInheritanceChain(typeof(BasicAppFullAuditedEntity)));
    }

    /// <summary>
    /// 聚合根基类的继承链必须经 AggregateRootBase 再落到 FullAuditedEntityBase。
    /// </summary>
    /// <remarks>
    /// 聚合根同时具备完整审计与领域事件容器；少了 AggregateRootBase 这一层，
    /// 事件容器消失且仓储不再把它当聚合根收集事件。
    /// </remarks>
    [Fact]
    public void BasicAppAggregateRoot_ShouldHaveExactInheritanceChain()
    {
        Assert.Equal(
            [
                typeof(BasicAppAggregateRoot),
                typeof(SugarMultiTenantAggregateRoot<long>),
                typeof(SugarAggregateRoot<long>),
                typeof(AggregateRootBase<long>),
                typeof(FullAuditedEntityBase<long>),
                typeof(EntityBase<long>),
                typeof(EntityBase)
            ],
            CoreTestHelper.GetInheritanceChain(typeof(BasicAppAggregateRoot)));
    }

    /// <summary>
    /// 聚合根基类必须可赋值给 <see cref="IAggregateRoot{TKey}"/>。
    /// </summary>
    /// <remarks>
    /// 仓储与工作单元是按聚合根接口挑出实体来收集领域事件的，接口丢失即事件静默不再发布。
    /// </remarks>
    [Fact]
    public void BasicAppAggregateRoot_ShouldBeAssignableToAggregateRootInterface()
    {
        Assert.True(typeof(BasicAppAggregateRoot).IsAssignableTo(typeof(IAggregateRoot<long>)));
        Assert.True(typeof(BasicAppAggregateRoot).IsAssignableTo(typeof(IAggregateRoot)));
    }

    /// <summary>
    /// 非聚合根的六个基类都不得可赋值给聚合根接口。
    /// </summary>
    /// <remarks>
    /// 否则普通实体会被工作单元当成聚合根去发布领域事件，产生凭空多出的事件流。
    /// </remarks>
    /// <param name="baseType">被检查的实体基类。</param>
    [Theory]
    [InlineData(typeof(BasicAppEntity))]
    [InlineData(typeof(BasicAppEntityWithIdentity))]
    [InlineData(typeof(BasicAppCreationEntity))]
    [InlineData(typeof(BasicAppModificationEntity))]
    [InlineData(typeof(BasicAppDeletionEntity))]
    [InlineData(typeof(BasicAppFullAuditedEntity))]
    public void NonAggregateRootBaseTypes_ShouldNotBeAssignableToAggregateRootInterface(Type baseType)
    {
        Assert.False(baseType.IsAssignableTo(typeof(IAggregateRoot)));
    }

    /// <summary>
    /// BasicId 的 setter 必须是 protected：外部只能在构造期指定主键，不得事后改写。
    /// </summary>
    /// <remarks>
    /// 主键可写意味着一条已持久化的记录能被"改嫁"到另一个 Id 上，仓储的脏检查与缓存键会全部失准。
    /// </remarks>
    /// <param name="baseType">被检查的实体基类。</param>
    [Theory]
    [MemberData(nameof(AllEntityBaseTypes))]
    public void BasicId_SetterShouldBeProtected(Type baseType)
    {
        var property = CoreTestHelper.RequireProperty(baseType, "BasicId");

        Assert.Equal(typeof(long), property.PropertyType);
        Assert.True(property.CanRead);
        Assert.True(property.GetGetMethod()!.IsPublic);
        Assert.Null(property.GetSetMethod());
        Assert.True(property.GetSetMethod(true)!.IsFamily, $"{baseType.Name}.BasicId 的 setter 必须是 protected。");
    }

    /// <summary>
    /// RowVersion 必须是公开可读写的非可空 long，供乐观并发校验读写。
    /// </summary>
    /// <param name="baseType">被检查的实体基类。</param>
    [Theory]
    [MemberData(nameof(AllEntityBaseTypes))]
    public void RowVersion_ShouldBePublicReadWriteInt64(Type baseType)
    {
        var property = CoreTestHelper.RequireProperty(baseType, "RowVersion");

        Assert.Equal(typeof(long), property.PropertyType);
        Assert.True(property.GetGetMethod()!.IsPublic);
        Assert.True(property.GetSetMethod()!.IsPublic);
    }

    /// <summary>
    /// 无参构造出来的实体是临时实体：主键为默认值、RowVersion 为 0。
    /// </summary>
    /// <remarks>
    /// 仓储用 <c>IsTransient()</c> 区分"新建/已持久化"，判反会让新实体走更新路径而写不进任何行。
    /// </remarks>
    [Fact]
    public void ParameterlessConstructor_ShouldProduceTransientEntity()
    {
        var entity = new CoreEntityProbe();

        Assert.Equal(0L, entity.BasicId);
        Assert.Equal(0L, entity.RowVersion);
        Assert.True(entity.IsTransient());
    }

    /// <summary>
    /// 带主键的构造函数必须把主键赋成给定值，并使实体不再是临时实体。
    /// </summary>
    [Fact]
    public void BasicIdConstructor_ShouldAssignBasicIdAndClearTransientFlag()
    {
        var entity = new CoreEntityProbe(9527L);

        Assert.Equal(9527L, entity.BasicId);
        Assert.False(entity.IsTransient());
    }

    /// <summary>
    /// 通过反射回填受保护主键后，实体必须立即脱离临时状态。
    /// </summary>
    /// <remarks>
    /// 这正是 ORM 插入后回填主键的路径，测试夹具与生产行为共用同一条 setter。
    /// </remarks>
    [Fact]
    public void SetBasicId_ShouldClearTransientFlag()
    {
        var entity = new CoreEntityProbe();
        Assert.True(entity.IsTransient());

        CoreTestHelper.SetBasicId(entity, 42L);

        Assert.Equal(42L, entity.BasicId);
        Assert.False(entity.IsTransient());
    }

    /// <summary>
    /// 主键相同且类型相同的两个实体相等，运算符与 Equals 结论一致。
    /// </summary>
    [Fact]
    public void Equals_SameTypeSameBasicIdShouldBeEqual()
    {
        var left = new CoreEntityProbe(7L);
        var right = new CoreEntityProbe(7L);

        Assert.True(left.Equals(right));
        Assert.True(left.Equals((object)right));
        Assert.True(left == right);
        Assert.False(left != right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    /// <summary>
    /// 主键相同但派生类型不同的两个实体必须不相等。
    /// </summary>
    /// <remarks>
    /// 违反这一条会让缓存与集合去重把不同表的同 Id 记录混成同一条，是很难排查的串数据事故。
    /// </remarks>
    [Fact]
    public void Equals_DifferentTypeSameBasicIdShouldNotBeEqual()
    {
        var entity = new CoreEntityProbe(7L);
        var other = new CoreOtherEntityProbe(7L);

        Assert.False(entity.Equals(other));
        Assert.False(other.Equals(entity));
        Assert.False(entity == other);
        Assert.True(entity != other);
    }

    /// <summary>
    /// 主键不同的同类型实体不相等。
    /// </summary>
    [Fact]
    public void Equals_DifferentBasicIdShouldNotBeEqual()
    {
        var left = new CoreEntityProbe(1L);
        var right = new CoreEntityProbe(2L);

        Assert.False(left.Equals(right));
        Assert.True(left != right);
    }

    /// <summary>
    /// 两个临时实体即使字段全同也不相等（尚未持久化，没有身份）。
    /// </summary>
    [Fact]
    public void Equals_TwoTransientEntitiesShouldNotBeEqual()
    {
        var left = new CoreEntityProbe();
        var right = new CoreEntityProbe();

        Assert.False(left.Equals(right));
        Assert.True(left != right);
    }

    /// <summary>
    /// 实体与自身始终相等（引用相等短路）。
    /// </summary>
    [Fact]
    public void Equals_SameReferenceShouldBeEqualEvenWhenTransient()
    {
        var entity = new CoreEntityProbe();

        Assert.True(entity.Equals(entity));
#pragma warning disable CS1718 // 有意比较同一变量：验证运算符对引用相等的处理
        Assert.True(entity == entity);
#pragma warning restore CS1718
    }

    /// <summary>
    /// 与 null 比较必须为不相等，两个 null 之间相等。
    /// </summary>
    [Fact]
    public void Equals_NullComparisonShouldFollowOperatorContract()
    {
        var entity = new CoreEntityProbe(3L);
        CoreEntityProbe? nothing = null;
        CoreEntityProbe? alsoNothing = null;

        Assert.False(entity.Equals(nothing));
        Assert.False(entity.Equals((object?)null));
        Assert.False(entity == nothing);
        Assert.True(entity != nothing);
        Assert.True(nothing == alsoNothing);
        Assert.False(nothing != alsoNothing);
    }

    /// <summary>
    /// 已持久化实体的哈希码必须由"类型 + 主键"共同决定。
    /// </summary>
    [Fact]
    public void GetHashCode_PersistedEntityShouldCombineTypeAndBasicId()
    {
        var entity = new CoreEntityProbe(11L);

        Assert.Equal(HashCode.Combine(typeof(CoreEntityProbe), 11L), entity.GetHashCode());
    }

    /// <summary>
    /// 临时实体的哈希码不得使用主键，否则一堆 Id=0 的新实体会全部落进同一个哈希桶。
    /// </summary>
    /// <remarks>
    /// 采用两个临时实例哈希码不相同来反证：若实现里对临时实体也走
    /// <c>HashCode.Combine(GetType(), BasicId)</c>，两者必然相同。
    /// </remarks>
    [Fact]
    public void GetHashCode_TransientEntityShouldNotDependOnBasicId()
    {
        var left = new CoreEntityProbe();
        var right = new CoreEntityProbe();

        Assert.NotEqual(left.GetHashCode(), right.GetHashCode());
        Assert.Equal(left.GetHashCode(), left.GetHashCode());
    }
}
