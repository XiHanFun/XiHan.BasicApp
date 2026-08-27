// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Entities;

namespace XiHan.BasicApp.Core.Tests;

/// <summary>
/// BasicApp 实体基类家族的审计字段形状与默认值测试。
/// </summary>
/// <remarks>
/// 每个基类"有哪些审计列"决定了派生实体能建出什么索引、能不能软删。
/// 其中最关键的是一组**负向**断言：纯创建型（Creation）实体是硬删，不能有 IsDeleted。
/// 源码注释写得很清楚——"纯创建型关联/日志实体为硬删，无 IsDeleted，唯一索引保持原样"。
/// 若基类哪天冒出软删字段，几十张关联表的唯一索引（不含 IsDeleted）会与软删语义直接打架：
/// 记录删了但唯一键还占着，同一关联再也建不回来。
/// </remarks>
public sealed class BasicAppEntityAuditShapeTests
{
    private static readonly string[] CreationAuditProperties = ["CreatedTime", "CreatedId", "CreatedBy"];
    private static readonly string[] ModificationAuditProperties = ["ModifiedTime", "ModifiedId", "ModifiedBy"];
    private static readonly string[] DeletionAuditProperties = ["IsDeleted", "DeletedTime", "DeletedId", "DeletedBy"];

    /// <summary>
    /// 各基类应当具备的审计属性清单。
    /// </summary>
    public static TheoryData<Type, string> ExpectedAuditProperties
    {
        get
        {
            TheoryData<Type, string> data = [];
            foreach (var name in CreationAuditProperties)
            {
                data.Add(typeof(BasicAppCreationEntity), name);
                data.Add(typeof(BasicAppFullAuditedEntity), name);
                data.Add(typeof(BasicAppAggregateRoot), name);
            }

            foreach (var name in ModificationAuditProperties)
            {
                data.Add(typeof(BasicAppModificationEntity), name);
                data.Add(typeof(BasicAppFullAuditedEntity), name);
                data.Add(typeof(BasicAppAggregateRoot), name);
            }

            foreach (var name in DeletionAuditProperties)
            {
                data.Add(typeof(BasicAppDeletionEntity), name);
                data.Add(typeof(BasicAppFullAuditedEntity), name);
                data.Add(typeof(BasicAppAggregateRoot), name);
            }

            return data;
        }
    }

    /// <summary>
    /// 各基类**不得**具备的审计属性清单。
    /// </summary>
    public static TheoryData<Type, string> ForbiddenAuditProperties
    {
        get
        {
            TheoryData<Type, string> data = [];
            foreach (var name in CreationAuditProperties.Concat(ModificationAuditProperties).Concat(DeletionAuditProperties))
            {
                data.Add(typeof(BasicAppEntity), name);
                data.Add(typeof(BasicAppEntityWithIdentity), name);
            }

            foreach (var name in ModificationAuditProperties.Concat(DeletionAuditProperties))
            {
                data.Add(typeof(BasicAppCreationEntity), name);
            }

            foreach (var name in CreationAuditProperties.Concat(DeletionAuditProperties))
            {
                data.Add(typeof(BasicAppModificationEntity), name);
            }

            foreach (var name in CreationAuditProperties.Concat(ModificationAuditProperties))
            {
                data.Add(typeof(BasicAppDeletionEntity), name);
            }

            return data;
        }
    }

    /// <summary>
    /// 基类必须具备其审计语义对应的全部属性。
    /// </summary>
    /// <remarks>
    /// 少任何一项都会让派生实体的既有索引（IX_{table}_TeId_CrTi、IX_{table}_CrId、IX_{table}_TeId_IsDe）
    /// 指向不存在的列，CodeFirst 建表阶段才会炸。
    /// </remarks>
    /// <param name="baseType">被检查的实体基类。</param>
    /// <param name="propertyName">应当存在的审计属性名。</param>
    [Theory]
    [MemberData(nameof(ExpectedAuditProperties))]
    public void AuditProperties_ShouldExistOnMatchingBaseType(Type baseType, string propertyName)
    {
        var property = CoreTestHelper.FindProperty(baseType, propertyName);

        Assert.True(property is not null, $"{baseType.Name} 缺少审计属性 {propertyName}。");
    }

    /// <summary>
    /// 基类不得混入其审计语义之外的属性。
    /// </summary>
    /// <remarks>
    /// 例如 <see cref="BasicAppCreationEntity"/> 出现 IsDeleted 就意味着硬删约定被破坏，
    /// 出现 ModifiedTime 则意味着有人在往只写表上做更新。
    /// </remarks>
    /// <param name="baseType">被检查的实体基类。</param>
    /// <param name="propertyName">不得存在的审计属性名。</param>
    [Theory]
    [MemberData(nameof(ForbiddenAuditProperties))]
    public void AuditProperties_ShouldNotLeakIntoUnrelatedBaseType(Type baseType, string propertyName)
    {
        Assert.True(
            CoreTestHelper.FindProperty(baseType, propertyName) is null,
            $"{baseType.Name} 不应具备 {propertyName}，它属于另一种审计语义。");
    }

    /// <summary>
    /// 创建型实体基类必须是硬删：绝不能出现软删标记。
    /// </summary>
    /// <remarks>
    /// 单列一条是因为这是本文件最贵的一条负向约束，且与唯一索引口径直接绑定：
    /// 创建型实体的唯一索引末列不附加 IsDeleted，一旦基类支持软删，删掉的行仍占着唯一键。
    /// </remarks>
    [Fact]
    public void BasicAppCreationEntity_ShouldNotSupportSoftDelete()
    {
        Assert.Null(CoreTestHelper.FindProperty(typeof(BasicAppCreationEntity), "IsDeleted"));
        Assert.Null(CoreTestHelper.FindProperty(typeof(BasicAppCreationEntity), "DeletedTime"));
    }

    /// <summary>
    /// CreatedTime 必须是非可空 <see cref="DateTimeOffset"/>，其余审计时间列必须可空。
    /// </summary>
    /// <remarks>
    /// 创建时间在插入时一定有值，可空化会引入"创建时间未知"的第三态；
    /// 修改/删除时间反过来，未发生时必须是 NULL 而不是某个纪元时刻。
    /// </remarks>
    [Fact]
    public void AuditTimeProperties_ShouldUseExpectedNullability()
    {
        Assert.Equal(
            typeof(DateTimeOffset),
            CoreTestHelper.RequireProperty(typeof(BasicAppFullAuditedEntity), "CreatedTime").PropertyType);
        Assert.Equal(
            typeof(DateTimeOffset?),
            CoreTestHelper.RequireProperty(typeof(BasicAppFullAuditedEntity), "ModifiedTime").PropertyType);
        Assert.Equal(
            typeof(DateTimeOffset?),
            CoreTestHelper.RequireProperty(typeof(BasicAppFullAuditedEntity), "DeletedTime").PropertyType);
    }

    /// <summary>
    /// IsDeleted 必须是非可空 bool。
    /// </summary>
    /// <remarks>
    /// 可空化会让"未删除"出现 NULL 第三态，所有 <c>WHERE is_deleted = false</c> 的查询都会漏掉这些行，
    /// 同时让"软删唯一索引末列附加 IsDeleted"的约定失效。
    /// </remarks>
    /// <param name="baseType">支持软删的实体基类。</param>
    [Theory]
    [InlineData(typeof(BasicAppDeletionEntity))]
    [InlineData(typeof(BasicAppFullAuditedEntity))]
    [InlineData(typeof(BasicAppAggregateRoot))]
    public void IsDeleted_ShouldBeNonNullableBoolean(Type baseType)
    {
        var property = CoreTestHelper.RequireProperty(baseType, "IsDeleted");

        Assert.Equal(typeof(bool), property.PropertyType);
        Assert.True(property.GetSetMethod()!.IsPublic);
    }

    /// <summary>
    /// 审计者 Id 列在 long 主键下落地为**非可空** long，"无审计者"表现为 0 而不是 NULL。
    /// </summary>
    /// <remarks>
    /// 框架把它声明成 <c>TKey?</c>，但 TKey 只有 <c>IEquatable&lt;TKey&gt;</c> 约束、没有 class/struct 约束，
    /// 因此对值类型 long 而言 <c>TKey?</c> 只是可空注解、不会变成 <see cref="Nullable{T}"/>。
    /// 结果是：列声明为 IsNullable=true，但实体端永远给不出 null，未指定创建者时写入的是 0。
    /// 这条断言锁定当前真实行为，避免有人按"可空"直觉写出 <c>CreatedId is null</c> 之类永假的判断。
    /// </remarks>
    /// <param name="propertyName">审计者 Id 属性名。</param>
    [Theory]
    [InlineData("CreatedId")]
    [InlineData("ModifiedId")]
    [InlineData("DeletedId")]
    public void AuditActorIdProperties_ShouldBeNonNullableInt64(string propertyName)
    {
        var property = CoreTestHelper.RequireProperty(typeof(BasicAppFullAuditedEntity), propertyName);

        Assert.Equal(typeof(long), property.PropertyType);
    }

    /// <summary>
    /// 审计者姓名列必须是可空字符串，默认 null。
    /// </summary>
    /// <param name="propertyName">审计者姓名属性名。</param>
    [Theory]
    [InlineData("CreatedBy")]
    [InlineData("ModifiedBy")]
    [InlineData("DeletedBy")]
    public void AuditActorNameProperties_ShouldBeNullableString(string propertyName)
    {
        var property = CoreTestHelper.RequireProperty(typeof(BasicAppFullAuditedEntity), propertyName);

        Assert.Equal(typeof(string), property.PropertyType);
        Assert.Null(property.GetValue(new CoreFullAuditedProbe()));
    }

    /// <summary>
    /// 创建型实体的 CreatedTime 默认必须是当前 UTC 时刻。
    /// </summary>
    /// <remarks>
    /// 只断言"偏移量为零"且"落在构造前后两次 UtcNow 之间"，不与任何绝对时刻比较，
    /// 因此不依赖机器时区、可并行任意顺序执行。若实现改用 <c>DateTimeOffset.Now</c>，
    /// 非 UTC 时区的机器上 Offset 立刻非零，本断言变红。
    /// </remarks>
    [Fact]
    public void BasicAppCreationEntity_CreatedTimeShouldDefaultToUtcNow()
    {
        var before = DateTimeOffset.UtcNow;
        var entity = new CoreCreationProbe();
        var after = DateTimeOffset.UtcNow;

        Assert.Equal(TimeSpan.Zero, entity.CreatedTime.Offset);
        Assert.InRange(entity.CreatedTime, before, after);
    }

    /// <summary>
    /// 完整审计实体的 CreatedTime 默认必须是当前 UTC 时刻。
    /// </summary>
    [Fact]
    public void BasicAppFullAuditedEntity_CreatedTimeShouldDefaultToUtcNow()
    {
        var before = DateTimeOffset.UtcNow;
        var entity = new CoreFullAuditedProbe();
        var after = DateTimeOffset.UtcNow;

        Assert.Equal(TimeSpan.Zero, entity.CreatedTime.Offset);
        Assert.InRange(entity.CreatedTime, before, after);
    }

    /// <summary>
    /// 聚合根的 CreatedTime 默认必须是当前 UTC 时刻。
    /// </summary>
    [Fact]
    public void BasicAppAggregateRoot_CreatedTimeShouldDefaultToUtcNow()
    {
        var before = DateTimeOffset.UtcNow;
        var entity = new CoreAggregateRootProbe();
        var after = DateTimeOffset.UtcNow;

        Assert.Equal(TimeSpan.Zero, entity.CreatedTime.Offset);
        Assert.InRange(entity.CreatedTime, before, after);
    }

    /// <summary>
    /// 带主键的构造函数同样要初始化 CreatedTime，不能只在无参构造里做。
    /// </summary>
    [Fact]
    public void BasicIdConstructor_ShouldStillInitializeCreatedTime()
    {
        var before = DateTimeOffset.UtcNow;
        var entity = new CoreFullAuditedProbe(66L);
        var after = DateTimeOffset.UtcNow;

        Assert.Equal(66L, entity.BasicId);
        Assert.InRange(entity.CreatedTime, before, after);
        Assert.False(entity.IsDeleted);
    }

    /// <summary>
    /// 新建的创建型实体不得携带任何创建者信息。
    /// </summary>
    /// <remarks>
    /// 创建者由服务层从当前用户上下文填充；基类预置任何非零/非空值都会伪造审计。
    /// </remarks>
    [Fact]
    public void BasicAppCreationEntity_ShouldNotPresetCreatorFields()
    {
        var entity = new CoreCreationProbe();

        Assert.Equal(0L, entity.CreatedId);
        Assert.Null(entity.CreatedBy);
    }

    /// <summary>
    /// 新建的修改型实体必须"未被修改过"：修改时间为 null。
    /// </summary>
    [Fact]
    public void BasicAppModificationEntity_ShouldStartUnmodified()
    {
        var entity = new CoreModificationProbe();

        Assert.Null(entity.ModifiedTime);
        Assert.Equal(0L, entity.ModifiedId);
        Assert.Null(entity.ModifiedBy);
    }

    /// <summary>
    /// 新建的删除型实体必须"未被删除"：IsDeleted 为 false 且删除审计全空。
    /// </summary>
    [Fact]
    public void BasicAppDeletionEntity_ShouldStartUndeleted()
    {
        var entity = new CoreDeletionProbe();

        Assert.False(entity.IsDeleted);
        Assert.Null(entity.DeletedTime);
        Assert.Equal(0L, entity.DeletedId);
        Assert.Null(entity.DeletedBy);
    }

    /// <summary>
    /// 新建的完整审计实体必须未修改、未删除。
    /// </summary>
    [Fact]
    public void BasicAppFullAuditedEntity_ShouldStartUnmodifiedAndUndeleted()
    {
        var entity = new CoreFullAuditedProbe();

        Assert.Null(entity.ModifiedTime);
        Assert.Null(entity.DeletedTime);
        Assert.False(entity.IsDeleted);
    }

    /// <summary>
    /// 新建的聚合根必须未修改、未删除。
    /// </summary>
    [Fact]
    public void BasicAppAggregateRoot_ShouldStartUnmodifiedAndUndeleted()
    {
        var entity = new CoreAggregateRootProbe(88L);

        Assert.Equal(88L, entity.BasicId);
        Assert.Null(entity.ModifiedTime);
        Assert.Null(entity.DeletedTime);
        Assert.False(entity.IsDeleted);
    }
}
