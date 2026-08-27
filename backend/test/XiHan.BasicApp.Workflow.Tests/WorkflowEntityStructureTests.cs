// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using System.Reflection;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Workflow.Domain.Entities;
using XiHan.Framework.Domain.Entities.Abstracts;

namespace XiHan.BasicApp.Workflow.Tests;

/// <summary>
/// 工作流四张持久化表的结构约束测试（反射型，不连库）。
/// </summary>
/// <remarks>
/// 这里守三条只会在生产上才暴露的约定：
/// 一是 <c>[SugarTable]</c> 缺失——CodeFirst 直接不建表，模块启动后所有写入报"表不存在"；
/// 二是软删除实体的唯一索引漏了 <c>IsDeleted</c>——删掉一条草稿定义后同编码同版本再也建不出来，
/// 因为软删行仍占着唯一键；
/// 三是 <see cref="IMultiTenantEntity"/> 丢失——租户行过滤经 <c>AddTableFilter&lt;IMultiTenantEntity&gt;</c> 注册，
/// 接口没了就静默读到别的租户的流程实例。
/// </remarks>
public sealed class WorkflowEntityStructureTests
{
    /// <summary>
    /// 模块内全部持久化实体。
    /// </summary>
    public static TheoryData<Type> AllEntityTypes => [.. PersistedEntityTypes];

    /// <summary>
    /// 模块内全部持久化实体（登记清单，结构断言与清单一致性检查共用）。
    /// </summary>
    private static readonly Type[] PersistedEntityTypes =
    [
        typeof(SysWorkflowDefinition),
        typeof(SysWorkflowInstance),
        typeof(SysWorkflowNodeInstance),
        typeof(SysWorkflowBookmark)
    ];

    /// <summary>
    /// 模块程序集里派生自 BasicApp 实体基类家族的类型必须与本文件登记的四张表完全一致，
    /// 新增实体却漏登记结构约束时在这里变红。
    /// </summary>
    [Fact]
    public void ModuleAssembly_PersistedEntities_ShouldMatchRegisteredList()
    {
        var registered = PersistedEntityTypes
            .Select(type => type.FullName!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        var discovered = typeof(SysWorkflowDefinition).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => type.IsAssignableTo(typeof(BasicAppEntity)) || type.IsAssignableTo(typeof(BasicAppFullAuditedEntity)))
            .Select(type => type.FullName!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            registered.SequenceEqual(discovered, StringComparer.Ordinal),
            $"工作流模块的持久化实体清单与结构约束登记不一致。" +
            $"{Environment.NewLine}已登记：{string.Join("、", registered)}" +
            $"{Environment.NewLine}实际发现：{string.Join("、", discovered)}");
    }

    /// <summary>
    /// 四张表必须都带 <c>[SugarTable]</c>，且表名与描述非空——缺失则 CodeFirst 不建表。
    /// </summary>
    /// <param name="entityType">被检查的实体类型。</param>
    [Theory]
    [MemberData(nameof(AllEntityTypes))]
    public void Entity_ShouldDeclareSugarTable(Type entityType)
    {
        var table = entityType.GetCustomAttribute<SugarTable>();

        Assert.True(table is not null, $"{entityType.Name} 缺少 [SugarTable]，CodeFirst 不会为它建表。");
        Assert.False(
            string.IsNullOrWhiteSpace(table!.TableName),
            $"{entityType.Name} 的 [SugarTable] 未指定表名，落库表名会退化成类名。");
        Assert.False(
            string.IsNullOrWhiteSpace(table.TableDescription),
            $"{entityType.Name} 的 [SugarTable] 未填写表描述。");
    }

    /// <summary>
    /// 表名必须锁死为约定值：改名等于线上列/表改名，全部手写升级脚本立即失效。
    /// </summary>
    /// <param name="entityTypeName">实体类型名。</param>
    /// <param name="expectedTableName">期望表名。</param>
    [Theory]
    [InlineData(nameof(SysWorkflowDefinition), "Sys_Workflow_Definition")]
    [InlineData(nameof(SysWorkflowInstance), "Sys_Workflow_Instance")]
    [InlineData(nameof(SysWorkflowNodeInstance), "Sys_Workflow_Node_Instance")]
    [InlineData(nameof(SysWorkflowBookmark), "Sys_Workflow_Bookmark")]
    public void Entity_TableName_ShouldStayStable(string entityTypeName, string expectedTableName)
    {
        var entityType = ResolveEntityType(entityTypeName);

        Assert.Equal(expectedTableName, entityType.GetCustomAttribute<SugarTable>()!.TableName, StringComparer.Ordinal);
    }

    /// <summary>
    /// 四张表都必须实现多租户实体接口，否则 SqlSugar 的租户行过滤对它们静默失效。
    /// </summary>
    /// <param name="entityType">被检查的实体类型。</param>
    [Theory]
    [MemberData(nameof(AllEntityTypes))]
    public void Entity_ShouldImplementMultiTenantEntity(Type entityType)
    {
        Assert.True(
            entityType.IsAssignableTo(typeof(IMultiTenantEntity)),
            $"{entityType.Name} 未实现 IMultiTenantEntity，租户行过滤会对其静默失效，跨租户数据将被读出。");
    }

    /// <summary>
    /// 四张表的租户列都必须默认落在平台租户 0 上，未显式赋租户的行不得漂到某个真实租户名下。
    /// </summary>
    /// <param name="entityType">被检查的实体类型。</param>
    [Theory]
    [MemberData(nameof(AllEntityTypes))]
    public void Entity_TenantId_ShouldDefaultToPlatformZero(Type entityType)
    {
        var entity = (IMultiTenantEntity)Activator.CreateInstance(entityType)!;

        Assert.Equal(0L, entity.TenantId);
    }

    /// <summary>
    /// 每张表的索引名在类内必须唯一，重名索引在 CodeFirst 建表时会互相覆盖。
    /// </summary>
    /// <param name="entityType">被检查的实体类型。</param>
    [Theory]
    [MemberData(nameof(AllEntityTypes))]
    public void Entity_IndexNames_ShouldBeUnique(Type entityType)
    {
        var duplicated = entityType.GetCustomAttributes<SugarIndexAttribute>()
            .GroupBy(index => index.IndexName, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        Assert.True(
            duplicated.Count == 0,
            $"{entityType.Name} 存在重名索引：{string.Join("、", duplicated)}。");
    }

    /// <summary>
    /// 索引里引用的每个字段都必须是实体上真实存在的属性，避免索引指向已删除的列。
    /// </summary>
    /// <param name="entityType">被检查的实体类型。</param>
    [Theory]
    [MemberData(nameof(AllEntityTypes))]
    public void Entity_IndexFields_ShouldReferenceExistingProperties(Type entityType)
    {
        var propertyNames = entityType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        var missing = entityType.GetCustomAttributes<SugarIndexAttribute>()
            .SelectMany(index => index.IndexFields.Keys.Select(field => $"{index.IndexName}.{field}"))
            .Where(pair => !propertyNames.Contains(pair.Split('.')[1]))
            .ToList();

        Assert.True(
            missing.Count == 0,
            $"{entityType.Name} 的下列索引字段在实体上不存在：{string.Join("、", missing)}。");
    }

    /// <summary>
    /// 软删除实体的唯一索引必须包含 <c>IsDeleted</c>，否则删掉的行仍占着唯一键、同键无法重建。
    /// </summary>
    [Fact]
    public void SoftDeletedEntity_UniqueIndexes_ShouldIncludeIsDeleted()
    {
        var softDeleted = PersistedEntityTypes
            .Where(type => type.IsAssignableTo(typeof(ISoftDelete)))
            .ToList();

        Assert.NotEmpty(softDeleted);

        var violations = softDeleted
            .SelectMany(type => type.GetCustomAttributes<SugarIndexAttribute>()
                .Where(index => index.IsUnique)
                .Where(index => !index.IndexFields.Keys.Contains(nameof(ISoftDelete.IsDeleted), StringComparer.Ordinal))
                .Select(index => $"{type.Name}.{index.IndexName}"))
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"下列唯一索引未包含 IsDeleted，软删记录会永久占用唯一键：{string.Join("、", violations)}。");
    }

    /// <summary>
    /// 定义表是唯一的软删除表；三张引擎运行时表按注释约定硬删，不得混入软删基类。
    /// </summary>
    [Fact]
    public void RuntimeEntities_ShouldBeHardDeleted()
    {
        Assert.True(
            typeof(SysWorkflowDefinition).IsAssignableTo(typeof(ISoftDelete)),
            "定义表必须软删，草稿删除后仍需保留审计痕迹。");

        Type[] runtimeEntities =
        [
            typeof(SysWorkflowInstance),
            typeof(SysWorkflowNodeInstance),
            typeof(SysWorkflowBookmark)
        ];
        var violations = runtimeEntities
            .Where(type => type.IsAssignableTo(typeof(ISoftDelete)))
            .Select(type => type.Name)
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"下列引擎运行时表被改成了软删：{string.Join("、", violations)}。" +
            $"存储层的删除走物理删，软删后书签/节点实例会被引擎反复读到。");
    }

    /// <summary>
    /// 定义表的编码 + 版本唯一索引必须存在且带 IsDeleted，同编码多版本的核心约束就靠它。
    /// </summary>
    [Fact]
    public void DefinitionEntity_ShouldDeclareCodeVersionUniqueIndex()
    {
        var unique = typeof(SysWorkflowDefinition).GetCustomAttributes<SugarIndexAttribute>()
            .Where(index => index.IsUnique)
            .ToList();

        var index = Assert.Single(unique);
        Assert.Contains(nameof(SysWorkflowDefinition.Code), index.IndexFields.Keys, StringComparer.Ordinal);
        Assert.Contains(nameof(SysWorkflowDefinition.Version), index.IndexFields.Keys, StringComparer.Ordinal);
        Assert.Contains(nameof(ISoftDelete.IsDeleted), index.IndexFields.Keys, StringComparer.Ordinal);
    }

    /// <summary>
    /// 四个 JSON 真源列必须是大文本且非空：改成定长会在复杂流程上静默截断，改成可空则读取时炸 null。
    /// </summary>
    /// <param name="entityTypeName">实体类型名。</param>
    /// <param name="propertyName">JSON 真源属性名。</param>
    [Theory]
    [InlineData(nameof(SysWorkflowDefinition), nameof(SysWorkflowDefinition.DefinitionJson))]
    [InlineData(nameof(SysWorkflowInstance), nameof(SysWorkflowInstance.InstanceJson))]
    [InlineData(nameof(SysWorkflowNodeInstance), nameof(SysWorkflowNodeInstance.NodeInstanceJson))]
    [InlineData(nameof(SysWorkflowBookmark), nameof(SysWorkflowBookmark.BookmarkJson))]
    public void Entity_JsonSourceColumn_ShouldBeNonNullableBigString(string entityTypeName, string propertyName)
    {
        var entityType = ResolveEntityType(entityTypeName);
        var property = entityType.GetProperty(propertyName)!;
        var column = property.GetCustomAttribute<SugarColumn>();

        Assert.True(column is not null, $"{entityTypeName}.{propertyName} 缺少 [SugarColumn]。");
        Assert.Equal(StaticConfig.CodeFirst_BigString, column!.ColumnDataType, StringComparer.Ordinal);
        Assert.False(column.IsNullable, $"{entityTypeName}.{propertyName} 是 JSON 真源，不得可空。");
        Assert.Equal(typeof(string), property.PropertyType);
    }

    /// <summary>
    /// 主键构造函数必须把引擎标识写进 BasicId：引擎标识即数据库主键是全模块的映射前提。
    /// </summary>
    /// <param name="entityType">被检查的实体类型。</param>
    [Theory]
    [MemberData(nameof(AllEntityTypes))]
    public void Entity_KeyConstructor_ShouldAssignBasicId(Type entityType)
    {
        var constructor = entityType.GetConstructor([typeof(long)]);

        Assert.True(constructor is not null, $"{entityType.Name} 缺少 (long basicId) 构造函数，存储层无法用引擎标识作主键。");

        var entity = (IEntityBase<long>)constructor!.Invoke([123456789L]);

        Assert.Equal(123456789L, entity.BasicId);
    }

    /// <summary>
    /// 无参构造函数必须保留：SqlSugar 查询物化依赖它，删掉后所有读取抛异常。
    /// </summary>
    /// <param name="entityType">被检查的实体类型。</param>
    [Theory]
    [MemberData(nameof(AllEntityTypes))]
    public void Entity_ShouldKeepParameterlessConstructor(Type entityType)
    {
        Assert.True(
            entityType.GetConstructor(Type.EmptyTypes) is not null,
            $"{entityType.Name} 缺少无参构造函数，SqlSugar 物化查询结果时会失败。");
    }

    /// <summary>
    /// 三张引擎运行时表的创建时间列必须是非空 DateTime（引擎时钟），不得被审计基类的 DateTimeOffset 顶替。
    /// </summary>
    /// <param name="entityTypeName">实体类型名。</param>
    [Theory]
    [InlineData(nameof(SysWorkflowInstance))]
    [InlineData(nameof(SysWorkflowBookmark))]
    public void RuntimeEntity_CreationTime_ShouldBeNonNullableDateTime(string entityTypeName)
    {
        var entityType = ResolveEntityType(entityTypeName);
        var property = entityType.GetProperty("CreationTime")!;

        Assert.Equal(typeof(DateTime), property.PropertyType);
        Assert.False(property.GetCustomAttribute<SugarColumn>()!.IsNullable);
    }

    /// <summary>
    /// 按类型名解析工作流实体类型。
    /// </summary>
    /// <param name="entityTypeName">实体类型名。</param>
    /// <returns>实体类型。</returns>
    private static Type ResolveEntityType(string entityTypeName)
    {
        return typeof(SysWorkflowDefinition).Assembly
            .GetTypes()
            .Single(type => string.Equals(type.Name, entityTypeName, StringComparison.Ordinal));
    }
}
