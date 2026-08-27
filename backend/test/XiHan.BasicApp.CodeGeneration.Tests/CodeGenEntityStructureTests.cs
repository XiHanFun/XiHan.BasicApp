// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using SqlSugar;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.Core.Entities;
using XiHan.Framework.Domain.Entities.Abstracts;

namespace XiHan.BasicApp.CodeGeneration.Tests;

/// <summary>
/// 代码生成五个持久化实体的结构约束测试。
/// </summary>
/// <remarks>
/// 实体的结构问题几乎都是"静默生效"的：少了 <c>[SugarTable]</c> 建不出表；
/// 少了多租户接口租户行过滤会被 SqlSugar 悄悄跳过；导航属性忘了 <c>IsIgnore</c> 会被当成列去建；
/// 忘了 <c>JsonIgnore</c> 会把整棵关联对象序列化进接口返回。
/// 这些都不会报错，只会在某天以"数据串了"或"响应体暴涨"的形式出现。
/// 本文件把它们变成会红的断言，并在失败消息里逐条列出违规项。
/// </remarks>
public sealed class CodeGenEntityStructureTests
{
    /// <summary>
    /// 本模块的五个持久化实体（查表用）。
    /// </summary>
    private static readonly Type[] EntityTypes =
    [
        typeof(SysCodeGenDataSource),
        typeof(SysCodeGenHistory),
        typeof(SysCodeGenTable),
        typeof(SysCodeGenTableColumn),
        typeof(SysCodeGenTemplate)
    ];

    /// <summary>
    /// 本模块的五个持久化实体。
    /// </summary>
    public static TheoryData<Type> PersistentEntityTypes
    {
        get
        {
            var data = new TheoryData<Type>();
            foreach (var entityType in EntityTypes)
            {
                data.Add(entityType);
            }

            return data;
        }
    }

    /// <summary>
    /// 按类型名取实体类型。
    /// </summary>
    /// <param name="entityTypeName">实体类型名</param>
    private static Type EntityTypeByName(string entityTypeName)
        => EntityTypes.Single(type => string.Equals(type.Name, entityTypeName, StringComparison.Ordinal));

    /// <summary>
    /// 取实体自身声明的公共实例属性（分部类的两个文件都算在内）。
    /// </summary>
    /// <param name="entityType">实体类型</param>
    private static IReadOnlyList<PropertyInfo> DeclaredProperties(Type entityType)
    {
        return [.. entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)];
    }

    /// <summary>
    /// 五个实体必须都带 <c>[SugarTable]</c>，且表名与表描述都不为空。
    /// </summary>
    /// <param name="entityType">实体类型</param>
    [Theory]
    [MemberData(nameof(PersistentEntityTypes))]
    public void PersistentEntity_ShouldDeclareSugarTableWithNameAndDescription(Type entityType)
    {
        var table = entityType.GetCustomAttribute<SugarTable>();

        Assert.True(table is not null, $"{entityType.Name} 缺少 [SugarTable]，CodeFirst 不会为它建表。");
        Assert.False(string.IsNullOrWhiteSpace(table!.TableName), $"{entityType.Name} 的 [SugarTable] 未指定 TableName。");
        Assert.False(
            string.IsNullOrWhiteSpace(table.TableDescription),
            $"{entityType.Name} 的 [SugarTable] 未指定 TableDescription，库里看不出这张表是干什么的。");
    }

    /// <summary>
    /// 表名统一走 <c>Sys_CodeGen_</c> 前缀，避免与其它模块的表撞名。
    /// </summary>
    /// <param name="entityType">实体类型</param>
    [Theory]
    [MemberData(nameof(PersistentEntityTypes))]
    public void PersistentEntity_TableNameShouldUseModulePrefix(Type entityType)
    {
        var tableName = entityType.GetCustomAttribute<SugarTable>()!.TableName;

        Assert.StartsWith("Sys_CodeGen_", tableName, StringComparison.Ordinal);
    }

    /// <summary>
    /// 五个实体必须继承全审计基类，从而同时获得租户列、审计列与软删列。
    /// </summary>
    /// <param name="entityType">实体类型</param>
    [Theory]
    [MemberData(nameof(PersistentEntityTypes))]
    public void PersistentEntity_ShouldInheritFullAuditedBase(Type entityType)
    {
        Assert.True(
            entityType.IsAssignableTo(typeof(BasicAppFullAuditedEntity)),
            $"{entityType.Name} 未继承 BasicAppFullAuditedEntity，会丢掉租户/审计/软删语义。");
    }

    /// <summary>
    /// 租户行过滤按 <see cref="IMultiTenantEntity"/> 注册，接口丢了过滤会静默失效。
    /// </summary>
    /// <param name="entityType">实体类型</param>
    [Theory]
    [MemberData(nameof(PersistentEntityTypes))]
    public void PersistentEntity_ShouldImplementMultiTenantEntity(Type entityType)
    {
        Assert.True(
            entityType.IsAssignableTo(typeof(IMultiTenantEntity)),
            $"{entityType.Name} 未实现 IMultiTenantEntity，租户行过滤会对它静默失效。");
    }

    /// <summary>
    /// 代码生成的配置数据保持"读共享"口径，不得实现严格租户隔离接口。
    /// </summary>
    /// <param name="entityType">实体类型</param>
    [Theory]
    [MemberData(nameof(PersistentEntityTypes))]
    public void PersistentEntity_ShouldNotImplementStrictMultiTenantEntity(Type entityType)
    {
        Assert.False(
            entityType.IsAssignableTo(typeof(IStrictMultiTenantEntity)),
            $"{entityType.Name} 实现了 IStrictMultiTenantEntity，平台级的模板/操作字典会在租户态整体消失。");
    }

    /// <summary>
    /// 实体必须是 public 且非密封的分部类：SqlSugar 依赖虚属性，分部文件承载导航属性。
    /// </summary>
    /// <param name="entityType">实体类型</param>
    [Theory]
    [MemberData(nameof(PersistentEntityTypes))]
    public void PersistentEntity_ShouldBePublicNonSealedClass(Type entityType)
    {
        Assert.True(entityType.IsPublic, $"{entityType.Name} 不是 public，模块外无法引用。");
        Assert.False(entityType.IsSealed, $"{entityType.Name} 被密封，SqlSugar 的虚属性代理会失效。");
        Assert.Equal("XiHan.BasicApp.CodeGeneration.Domain.Entities", entityType.Namespace, StringComparer.Ordinal);
    }

    /// <summary>
    /// 实体自身声明的每个属性都必须带 <c>[SugarColumn]</c>，且都是 virtual。
    /// </summary>
    /// <param name="entityType">实体类型</param>
    [Theory]
    [MemberData(nameof(PersistentEntityTypes))]
    public void PersistentEntity_EveryDeclaredPropertyShouldBeVirtualWithSugarColumn(Type entityType)
    {
        var missingAttribute = DeclaredProperties(entityType)
            .Where(property => property.GetCustomAttribute<SugarColumn>() is null)
            .Select(property => property.Name)
            .ToList();
        var notVirtual = DeclaredProperties(entityType)
            .Where(property => property.GetGetMethod() is null || !property.GetGetMethod()!.IsVirtual)
            .Select(property => property.Name)
            .ToList();

        Assert.True(
            missingAttribute.Count == 0,
            $"{entityType.Name} 的以下属性缺少 [SugarColumn]，列名/长度/注释全靠约定推断：{string.Join("、", missingAttribute)}");
        Assert.True(
            notVirtual.Count == 0,
            $"{entityType.Name} 的以下属性不是 virtual：{string.Join("、", notVirtual)}");
    }

    /// <summary>
    /// 每个持久化列都必须写列描述，库结构自解释是本仓库的硬约定。
    /// </summary>
    /// <param name="entityType">实体类型</param>
    [Theory]
    [MemberData(nameof(PersistentEntityTypes))]
    public void PersistentEntity_EveryPersistedColumnShouldDeclareDescription(Type entityType)
    {
        var missing = DeclaredProperties(entityType)
            .Select(property => (property.Name, Column: property.GetCustomAttribute<SugarColumn>()))
            .Where(item => item.Column is { IsIgnore: false } && string.IsNullOrWhiteSpace(item.Column.ColumnDescription))
            .Select(item => item.Name)
            .ToList();

        Assert.True(
            missing.Count == 0,
            $"{entityType.Name} 的以下列缺少 ColumnDescription：{string.Join("、", missing)}");
    }

    /// <summary>
    /// 字符串列必须显式给长度或声明为大文本，否则各方言的默认长度会互相打架。
    /// </summary>
    /// <param name="entityType">实体类型</param>
    [Theory]
    [MemberData(nameof(PersistentEntityTypes))]
    public void PersistentEntity_StringColumnShouldDeclareLengthOrBigString(Type entityType)
    {
        var offenders = DeclaredProperties(entityType)
            .Select(property => (property.Name, property.PropertyType, Column: property.GetCustomAttribute<SugarColumn>()))
            .Where(item => item.PropertyType == typeof(string)
                && item.Column is { IsIgnore: false }
                && item.Column.Length <= 0
                && !string.Equals(item.Column.ColumnDataType, StaticConfig.CodeFirst_BigString, StringComparison.Ordinal))
            .Select(item => item.Name)
            .ToList();

        Assert.True(
            offenders.Count == 0,
            $"{entityType.Name} 的以下字符串列既没有 Length 也不是大文本：{string.Join("、", offenders)}");
    }

    /// <summary>
    /// 导航属性必须同时满足：不落库、不进 System.Text.Json、不进 Newtonsoft.Json。
    /// </summary>
    /// <remarks>
    /// 少了 <c>IsIgnore</c> 会被当成列去建表；少了任一 JsonIgnore，只要有人不小心把实体直接返回，
    /// 关联对象就会被整棵序列化出去——包括租户信息与操作用户。
    /// </remarks>
    /// <param name="entityType">实体类型</param>
    [Theory]
    [MemberData(nameof(PersistentEntityTypes))]
    public void PersistentEntity_NavigationPropertyShouldBeIgnoredEverywhere(Type entityType)
    {
        var offenders = new List<string>();
        foreach (var property in DeclaredProperties(entityType))
        {
            if (property.GetCustomAttribute<Navigate>() is null)
            {
                continue;
            }

            var column = property.GetCustomAttribute<SugarColumn>();
            if (column is not { IsIgnore: true })
            {
                offenders.Add($"{property.Name}（缺少 SugarColumn(IsIgnore = true)）");
            }

            if (property.GetCustomAttribute<System.Text.Json.Serialization.JsonIgnoreAttribute>() is null)
            {
                offenders.Add($"{property.Name}（缺少 System.Text.Json 的 JsonIgnore）");
            }

            if (property.GetCustomAttribute<Newtonsoft.Json.JsonIgnoreAttribute>() is null)
            {
                offenders.Add($"{property.Name}（缺少 Newtonsoft.Json 的 JsonIgnore）");
            }
        }

        Assert.True(offenders.Count == 0, $"{entityType.Name} 的导航属性未被完全排除：{string.Join("；", offenders)}");
    }

    /// <summary>
    /// 反过来：被标为不落库的属性必须确实是导航属性，避免真列被误标 IsIgnore 而永远存不进库。
    /// </summary>
    /// <param name="entityType">实体类型</param>
    [Theory]
    [MemberData(nameof(PersistentEntityTypes))]
    public void PersistentEntity_IgnoredPropertyShouldBeNavigation(Type entityType)
    {
        var offenders = DeclaredProperties(entityType)
            .Where(property => property.GetCustomAttribute<SugarColumn>() is { IsIgnore: true }
                && property.GetCustomAttribute<Navigate>() is null)
            .Select(property => property.Name)
            .ToList();

        Assert.True(
            offenders.Count == 0,
            $"{entityType.Name} 的以下属性被标为不落库却不是导航属性，赋了值也存不进库：{string.Join("、", offenders)}");
    }

    /// <summary>
    /// 五个实体的分部文件都必须挂上租户导航属性，这是 .pl.cs 分部存在的意义。
    /// </summary>
    /// <param name="entityType">实体类型</param>
    [Theory]
    [MemberData(nameof(PersistentEntityTypes))]
    public void PersistentEntity_ShouldDeclareTenantNavigation(Type entityType)
    {
        var tenant = entityType.GetProperty("Tenant", BindingFlags.Public | BindingFlags.Instance);

        Assert.True(tenant is not null, $"{entityType.Name} 缺少 Tenant 导航属性（应在 .pl.cs 分部里声明）。");
        Assert.NotNull(tenant!.GetCustomAttribute<Navigate>());
    }

    /// <summary>
    /// 每个实体都必须带三条基线索引：租户+创建时间、创建人、租户+软删。
    /// </summary>
    /// <remarks>
    /// 列表页按租户 + 创建时间倒序取数，软删过滤又永远带 IsDeleted；缺了这三条，
    /// 数据量一上来就是全表扫描。
    /// </remarks>
    /// <param name="entityType">实体类型</param>
    [Theory]
    [MemberData(nameof(PersistentEntityTypes))]
    public void PersistentEntity_ShouldDeclareBaselineIndexes(Type entityType)
    {
        var indexNames = entityType.GetCustomAttributes<SugarIndexAttribute>()
            .Select(index => index.IndexName)
            .ToList();

        var missing = new[] { "IX_{table}_TeId_CrTi", "IX_{table}_CrId", "IX_{table}_TeId_IsDe" }
            .Where(expected => !indexNames.Contains(expected, StringComparer.Ordinal))
            .ToList();

        Assert.True(
            missing.Count == 0,
            $"{entityType.Name} 缺少基线索引：{string.Join("、", missing)}；现有索引：{string.Join("、", indexNames)}");
    }

    /// <summary>
    /// 索引名必须使用 <c>{table}</c> 占位，否则跨表索引名会在同一库里撞掉。
    /// </summary>
    /// <param name="entityType">实体类型</param>
    [Theory]
    [MemberData(nameof(PersistentEntityTypes))]
    public void PersistentEntity_IndexNameShouldContainTablePlaceholder(Type entityType)
    {
        var offenders = entityType.GetCustomAttributes<SugarIndexAttribute>()
            .Select(index => index.IndexName)
            .Where(name => !name.Contains("{table}", StringComparison.Ordinal))
            .ToList();

        Assert.True(
            offenders.Count == 0,
            $"{entityType.Name} 的以下索引名未使用 {{table}} 占位：{string.Join("、", offenders)}");
    }

    /// <summary>
    /// 索引引用的列必须真实存在于实体上（含基类列），拼错列名要到建表时才炸。
    /// </summary>
    /// <param name="entityType">实体类型</param>
    [Theory]
    [MemberData(nameof(PersistentEntityTypes))]
    public void PersistentEntity_IndexFieldsShouldReferenceExistingProperties(Type entityType)
    {
        var propertyNames = entityType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        var offenders = entityType.GetCustomAttributes<SugarIndexAttribute>()
            .SelectMany(index => index.IndexFields.Keys.Select(field => (index.IndexName, Field: field)))
            .Where(item => !propertyNames.Contains(item.Field))
            .Select(item => $"{item.IndexName} → {item.Field}")
            .ToList();

        Assert.True(offenders.Count == 0, $"{entityType.Name} 的索引引用了不存在的属性：{string.Join("、", offenders)}");
    }

    /// <summary>
    /// 三个业务唯一键（数据源名 / 表名 / 模板编码）当前是"全库唯一"，既不含租户也不含软删标记。
    /// </summary>
    /// <remarks>
    /// 这是锁定当前真实行为的回归锚点，不是对该设计的背书：Saas 侧同类实体的唯一索引一律是
    /// <c>(TenantId, 业务码, IsDeleted)</c> 三段式。当前形态意味着软删一条数据源后无法再用同名新建，
    /// 且两个租户不能配置同一张表。若后续改为三段式，本用例会红，届时应改为断言含 TenantId 与 IsDeleted。
    /// </remarks>
    /// <param name="entityTypeName">实体类型名</param>
    /// <param name="indexName">唯一索引名</param>
    /// <param name="expectedField">唯一索引当前包含的唯一列</param>
    [Theory]
    [InlineData(nameof(SysCodeGenDataSource), "UX_{table}_SoNa", nameof(SysCodeGenDataSource.SourceName))]
    [InlineData(nameof(SysCodeGenTable), "UX_{table}_TaNa", nameof(SysCodeGenTable.TableName))]
    [InlineData(nameof(SysCodeGenTemplate), "UX_{table}_TeCo", nameof(SysCodeGenTemplate.TemplateCode))]
    public void PersistentEntity_BusinessUniqueIndexIsCurrentlyGlobal(string entityTypeName, string indexName, string expectedField)
    {
        var entityType = EntityTypeByName(entityTypeName);
        var index = entityType.GetCustomAttributes<SugarIndexAttribute>()
            .Single(item => string.Equals(item.IndexName, indexName, StringComparison.Ordinal));

        Assert.True(index.IsUnique, $"{entityTypeName}.{indexName} 必须是唯一索引。");
        Assert.Equal([expectedField], index.IndexFields.Keys);
    }

    /// <summary>
    /// 历史记录实体不带业务唯一索引：同一张表可以反复生成，每次追加一条。
    /// </summary>
    [Fact]
    public void History_ShouldNotDeclareAnyUniqueIndex()
    {
        var uniqueIndexes = typeof(SysCodeGenHistory).GetCustomAttributes<SugarIndexAttribute>()
            .Where(index => index.IsUnique)
            .Select(index => index.IndexName)
            .ToList();

        Assert.True(
            uniqueIndexes.Count == 0,
            $"SysCodeGenHistory 出现了唯一索引，会挡住同一张表的重复生成：{string.Join("、", uniqueIndexes)}");
    }

    /// <summary>
    /// 列配置与历史记录必须持有指向表配置的外键列，且都建了检索索引。
    /// </summary>
    /// <param name="entityTypeName">实体类型名</param>
    /// <param name="indexName">索引名</param>
    [Theory]
    [InlineData(nameof(SysCodeGenTableColumn), "IX_{table}_TaId")]
    [InlineData(nameof(SysCodeGenHistory), "IX_{table}_TeId_TaId")]
    public void ChildEntity_ShouldIndexOwningTableId(string entityTypeName, string indexName)
    {
        var entityType = EntityTypeByName(entityTypeName);

        Assert.NotNull(entityType.GetProperty("TableId", BindingFlags.Public | BindingFlags.Instance));
        Assert.Contains(
            entityType.GetCustomAttributes<SugarIndexAttribute>(),
            index => string.Equals(index.IndexName, indexName, StringComparison.Ordinal));
    }

    /// <summary>
    /// 表配置与列配置都必须带"已人工修改字段"列，dirty-tracking 的冻结机制依赖它落库。
    /// </summary>
    /// <param name="entityType">实体类型</param>
    [Theory]
    [InlineData(typeof(SysCodeGenTable))]
    [InlineData(typeof(SysCodeGenTableColumn))]
    public void DirtyTrackedEntity_ShouldDeclareUserModifiedFieldsColumn(Type entityType)
    {
        var property = entityType.GetProperty("UserModifiedFields", BindingFlags.Public | BindingFlags.Instance);

        Assert.True(property is not null, $"{entityType.Name} 缺少 UserModifiedFields，同步表结构会冲掉人工配置。");
        Assert.Equal(typeof(string), property!.PropertyType);
        Assert.True(property.GetCustomAttribute<SugarColumn>()!.Length > 0);
    }
}
