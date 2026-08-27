// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using System.Reflection;
using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.Core.Entities;
using XiHan.Framework.Domain.Entities.Abstracts;

namespace XiHan.BasicApp.AI.Tests;

/// <summary>
/// AI 模块四张持久化表的结构约束测试（反射型，不连库）。
/// </summary>
/// <remarks>
/// 守的是三条只在生产才暴露的约定：
/// 一是 <c>[SugarTable]</c> 缺失——CodeFirst 直接不建表，模块启动后所有写入报"表不存在"；
/// 二是软删除实体的唯一索引漏了 <c>IsDeleted</c>——软删一条 provider 后同编码再也建不出来，因为软删行仍占着唯一键；
/// 三是 <see cref="IMultiTenantEntity"/> 丢失——租户行过滤经 <c>AddTableFilter&lt;IMultiTenantEntity&gt;</c> 注册，
/// 接口没了就静默读到别的租户的模型密钥与知识库原文。
/// </remarks>
public sealed class AiEntityStructureTests
{
    /// <summary>
    /// 模块内全部持久化实体（登记清单，结构断言与清单一致性检查共用）。
    /// </summary>
    private static readonly Type[] PersistedEntityTypes =
    [
        typeof(SysAiAssistant),
        typeof(SysAiPrompt),
        typeof(SysAiProvider),
        typeof(SysKnowledgeDocument)
    ];

    /// <summary>
    /// 模块内全部持久化实体（供 [Theory] 逐张表检查）。
    /// </summary>
    public static TheoryData<Type> AllEntityTypes
    {
        get
        {
            var data = new TheoryData<Type>();
            foreach (var type in PersistedEntityTypes)
            {
                data.Add(type);
            }

            return data;
        }
    }

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
        var discovered = typeof(SysAiProvider).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => type.IsAssignableTo(typeof(BasicAppEntity)) || type.IsAssignableTo(typeof(BasicAppFullAuditedEntity)))
            .Select(type => type.FullName!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(registered, discovered, StringComparer.Ordinal);
    }

    /// <summary>
    /// 每张表都必须带 <c>[SugarTable]</c> 且给出物理表名与中文表说明，缺一个 CodeFirst 就建不出可读的表。
    /// </summary>
    /// <param name="entityType">被检查的实体类型。</param>
    [Theory]
    [MemberData(nameof(AllEntityTypes))]
    public void Entity_ShouldDeclareSugarTableWithNameAndDescription(Type entityType)
    {
        var table = entityType.GetCustomAttribute<SugarTable>();

        Assert.True(table is not null, $"{entityType.Name} 缺少 [SugarTable]，CodeFirst 不会为它建表。");
        Assert.False(string.IsNullOrWhiteSpace(table!.TableName), $"{entityType.Name} 的 [SugarTable] 未指定物理表名。");
        Assert.False(string.IsNullOrWhiteSpace(table.TableDescription), $"{entityType.Name} 的 [SugarTable] 未指定表说明。");
    }

    /// <summary>
    /// 四张表的物理表名一经上线不可变更，改名等于把线上数据整表丢弃。
    /// </summary>
    /// <param name="entityTypeName">实体类型名。</param>
    /// <param name="tableName">期望的物理表名。</param>
    [Theory]
    [InlineData(nameof(SysAiAssistant), "Sys_Ai_Assistant")]
    [InlineData(nameof(SysAiPrompt), "Sys_Ai_Prompt")]
    [InlineData(nameof(SysAiProvider), "Sys_Ai_Provider")]
    [InlineData(nameof(SysKnowledgeDocument), "Sys_Knowledge_Document")]
    public void Entity_TableName_ShouldBeStable(string entityTypeName, string tableName)
    {
        var entityType = PersistedEntityTypes.Single(type => string.Equals(type.Name, entityTypeName, StringComparison.Ordinal));

        Assert.Equal(tableName, entityType.GetCustomAttribute<SugarTable>()!.TableName, StringComparer.Ordinal);
    }

    /// <summary>
    /// 四张表的物理表名必须互不相同，撞名会让两个实体读写同一张表。
    /// </summary>
    [Fact]
    public void Entity_TableNames_ShouldBeUniqueAcrossModule()
    {
        var duplicated = PersistedEntityTypes
            .Select(type => type.GetCustomAttribute<SugarTable>()!.TableName)
            .GroupBy(name => name, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        Assert.True(duplicated.Count == 0, $"物理表名重复：{string.Join("、", duplicated)}。");
    }

    /// <summary>
    /// 四张表必须全部实现 <see cref="IMultiTenantEntity"/>，这是租户行过滤生效的唯一前提。
    /// </summary>
    /// <param name="entityType">被检查的实体类型。</param>
    [Theory]
    [MemberData(nameof(AllEntityTypes))]
    public void Entity_ShouldImplementMultiTenantEntity(Type entityType)
    {
        Assert.True(
            entityType.IsAssignableTo(typeof(IMultiTenantEntity)),
            $"{entityType.Name} 未实现 IMultiTenantEntity，租户行过滤会对它静默失效。");
    }

    /// <summary>
    /// 新建实例的 TenantId 必须是 0（平台/全局），由租户上下文在写入时接管，不得用可空租户 id 表达全局。
    /// </summary>
    /// <param name="entityType">被检查的实体类型。</param>
    [Theory]
    [MemberData(nameof(AllEntityTypes))]
    public void Entity_NewInstanceTenantId_ShouldDefaultToPlatform(Type entityType)
    {
        var entity = (IMultiTenantEntity)Activator.CreateInstance(entityType)!;

        Assert.Equal(0L, entity.TenantId);
    }

    /// <summary>
    /// 四张表必须全部软删（继承全审计基类），硬删会让密钥配置与知识文档的变更痕迹彻底丢失。
    /// </summary>
    /// <param name="entityType">被检查的实体类型。</param>
    [Theory]
    [MemberData(nameof(AllEntityTypes))]
    public void Entity_ShouldBeSoftDeletedFullAudited(Type entityType)
    {
        Assert.True(
            entityType.IsAssignableTo(typeof(BasicAppFullAuditedEntity)),
            $"{entityType.Name} 不是全审计实体，创建人/修改人/软删标记会整体缺失。");
        Assert.True(
            entityType.IsAssignableTo(typeof(ISoftDelete)),
            $"{entityType.Name} 未实现 ISoftDelete，删除会变成物理删。");
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

        Assert.True(duplicated.Count == 0, $"{entityType.Name} 存在重名索引：{string.Join("、", duplicated)}。");
    }

    /// <summary>
    /// 索引名必须带 <c>{table}</c> 占位符，写死表名会让同库多模块的索引撞名。
    /// </summary>
    /// <param name="entityType">被检查的实体类型。</param>
    [Theory]
    [MemberData(nameof(AllEntityTypes))]
    public void Entity_IndexNames_ShouldUseTablePlaceholder(Type entityType)
    {
        var violations = entityType.GetCustomAttributes<SugarIndexAttribute>()
            .Where(index => !index.IndexName.Contains("{table}", StringComparison.Ordinal))
            .Select(index => index.IndexName)
            .ToList();

        Assert.True(violations.Count == 0, $"{entityType.Name} 的下列索引名未使用 {{table}} 占位符：{string.Join("、", violations)}。");
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
            .SelectMany(index => index.IndexFields.Keys.Select(field => (Index: index.IndexName, Field: field)))
            .Where(pair => !propertyNames.Contains(pair.Field))
            .Select(pair => $"{pair.Index}.{pair.Field}")
            .ToList();

        Assert.True(missing.Count == 0, $"{entityType.Name} 的下列索引字段在实体上不存在：{string.Join("、", missing)}。");
    }

    /// <summary>
    /// 每张表都必须有一条以 TenantId 打头的查询索引，缺了它租户过滤后的列表查询会退化成全表扫。
    /// </summary>
    /// <param name="entityType">被检查的实体类型。</param>
    [Theory]
    [MemberData(nameof(AllEntityTypes))]
    public void Entity_ShouldDeclareTenantScopedIndexes(Type entityType)
    {
        var tenantIndexes = entityType.GetCustomAttributes<SugarIndexAttribute>()
            .Where(index => index.IndexFields.Keys.Contains(nameof(IMultiTenantEntity.TenantId), StringComparer.Ordinal))
            .ToList();

        Assert.True(tenantIndexes.Count > 0, $"{entityType.Name} 没有任何以 TenantId 参与的索引。");
    }

    /// <summary>
    /// 软删除表的唯一索引必须同时包含 TenantId 与 IsDeleted：
    /// 缺 TenantId 会让编码在全平台唯一（租户之间互相占用编码），缺 IsDeleted 会让软删行永久占着唯一键。
    /// </summary>
    [Fact]
    public void Entity_UniqueIndexes_ShouldIncludeTenantIdAndIsDeleted()
    {
        var violations = PersistedEntityTypes
            .SelectMany(type => type.GetCustomAttributes<SugarIndexAttribute>()
                .Where(index => index.IsUnique)
                .Where(index =>
                    !index.IndexFields.Keys.Contains(nameof(IMultiTenantEntity.TenantId), StringComparer.Ordinal) ||
                    !index.IndexFields.Keys.Contains(nameof(ISoftDelete.IsDeleted), StringComparer.Ordinal))
                .Select(index => $"{type.Name}.{index.IndexName}"))
            .ToList();

        Assert.True(violations.Count == 0, $"下列唯一索引缺少 TenantId 或 IsDeleted：{string.Join("、", violations)}。");
    }

    /// <summary>
    /// 三张带业务编码的表必须各有且仅有一条唯一索引，且落在自己的编码列上；
    /// 知识文档按主键关联向量库、无业务编码，故不得有唯一索引。
    /// </summary>
    [Fact]
    public void Entity_BusinessCodeUniqueIndexes_ShouldCoverEachCodeColumn()
    {
        AssertSingleUniqueIndexOn(typeof(SysAiAssistant), nameof(SysAiAssistant.AssistantCode));
        AssertSingleUniqueIndexOn(typeof(SysAiPrompt), nameof(SysAiPrompt.PromptCode));
        AssertSingleUniqueIndexOn(typeof(SysAiProvider), nameof(SysAiProvider.ConfigCode));

        Assert.DoesNotContain(typeof(SysKnowledgeDocument).GetCustomAttributes<SugarIndexAttribute>(), index => index.IsUnique);
    }

    /// <summary>
    /// 每个公共可写属性都必须带 <c>[SugarColumn]</c>，漏标会让 CodeFirst 按属性名直接造列、与既有表列名对不上。
    /// </summary>
    /// <param name="entityType">被检查的实体类型。</param>
    [Theory]
    [MemberData(nameof(AllEntityTypes))]
    public void Entity_DeclaredProperties_ShouldCarrySugarColumn(Type entityType)
    {
        var missing = entityType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(property => property.CanWrite)
            .Where(property => property.GetCustomAttribute<SugarColumn>() is null)
            .Select(property => property.Name)
            .ToList();

        Assert.True(missing.Count == 0, $"{entityType.Name} 的下列属性缺少 [SugarColumn]：{string.Join("、", missing)}。");
    }

    /// <summary>
    /// 每个列名必须显式给出且在类内唯一，重名列会让两个属性映射到同一列而互相覆盖。
    /// </summary>
    /// <param name="entityType">被检查的实体类型。</param>
    [Theory]
    [MemberData(nameof(AllEntityTypes))]
    public void Entity_ColumnNames_ShouldBeExplicitAndUnique(Type entityType)
    {
        var columns = entityType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(property => property.CanWrite)
            .Select(property => (property.Name, Column: property.GetCustomAttribute<SugarColumn>()?.ColumnName))
            .ToList();
        var missing = columns.Where(item => string.IsNullOrWhiteSpace(item.Column)).Select(item => item.Name).ToList();
        var duplicated = columns
            .Where(item => !string.IsNullOrWhiteSpace(item.Column))
            .GroupBy(item => item.Column!, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        Assert.True(missing.Count == 0, $"{entityType.Name} 的下列属性未显式指定列名：{string.Join("、", missing)}。");
        Assert.True(duplicated.Count == 0, $"{entityType.Name} 的下列列名重复：{string.Join("、", duplicated)}。");
    }

    /// <summary>
    /// 密钥列必须同时挂上两套 JsonIgnore：只挂一套时另一个序列化器仍会把密文吐给前端。
    /// </summary>
    [Fact]
    public void ProviderApiKey_ShouldBeIgnoredByBothSerializers()
    {
        var property = typeof(SysAiProvider).GetProperty(nameof(SysAiProvider.ApiKey))!;
        var attributeNames = property
            .GetCustomAttributes(inherit: true)
            .Select(attribute => attribute.GetType().FullName!)
            .ToList();

        Assert.Contains("Newtonsoft.Json.JsonIgnoreAttribute", attributeNames, StringComparer.Ordinal);
        Assert.Contains("System.Text.Json.Serialization.JsonIgnoreAttribute", attributeNames, StringComparer.Ordinal);
    }

    /// <summary>
    /// 密钥列必须可空且留足密文长度：Data Protection 密文远长于明文，列长不足会在写入时直接截断/报错。
    /// </summary>
    [Fact]
    public void ProviderApiKey_ShouldBeNullableWithCipherSizedLength()
    {
        var column = typeof(SysAiProvider).GetProperty(nameof(SysAiProvider.ApiKey))!.GetCustomAttribute<SugarColumn>()!;

        Assert.True(column.IsNullable, "ApiKey 必须可空：未配置密钥是合法状态。");
        Assert.Equal(500, column.Length);
        Assert.Equal("Api_Key", column.ColumnName, StringComparer.Ordinal);
    }

    /// <summary>
    /// 三处大文本列必须声明为 BigString 且不设定长，改成定长会在长提示词/长文档上静默截断。
    /// </summary>
    /// <param name="entityTypeName">实体类型名。</param>
    /// <param name="propertyName">大文本属性名。</param>
    [Theory]
    [InlineData(nameof(SysAiPrompt), nameof(SysAiPrompt.Content))]
    [InlineData(nameof(SysAiProvider), nameof(SysAiProvider.ExtraJson))]
    [InlineData(nameof(SysKnowledgeDocument), nameof(SysKnowledgeDocument.RawContent))]
    public void Entity_BigStringColumns_ShouldUseCodeFirstBigString(string entityTypeName, string propertyName)
    {
        var entityType = PersistedEntityTypes.Single(type => string.Equals(type.Name, entityTypeName, StringComparison.Ordinal));
        var column = entityType.GetProperty(propertyName)!.GetCustomAttribute<SugarColumn>()!;

        Assert.Equal(StaticConfig.CodeFirst_BigString, column.ColumnDataType, StringComparer.Ordinal);
        Assert.Equal(0, column.Length);
    }

    /// <summary>
    /// 提示词正文与知识原文是各自表的真源，必须非空；provider 的扩展 JSON 是可选扩展，必须可空。
    /// </summary>
    [Fact]
    public void Entity_BigStringNullability_ShouldMatchItsRole()
    {
        Assert.False(
            typeof(SysAiPrompt).GetProperty(nameof(SysAiPrompt.Content))!.GetCustomAttribute<SugarColumn>()!.IsNullable,
            "提示词正文是真源，必须非空。");
        Assert.False(
            typeof(SysKnowledgeDocument).GetProperty(nameof(SysKnowledgeDocument.RawContent))!.GetCustomAttribute<SugarColumn>()!.IsNullable,
            "知识原文是重建索引的唯一依据，必须非空。");
        Assert.True(
            typeof(SysAiProvider).GetProperty(nameof(SysAiProvider.ExtraJson))!.GetCustomAttribute<SugarColumn>()!.IsNullable,
            "扩展 JSON 是可选项，必须可空。");
    }

    /// <summary>
    /// 领域服务的长度校验必须与列长逐个对齐；对不上就会出现"校验放行、落库截断"。
    /// </summary>
    /// <param name="entityTypeName">实体类型名。</param>
    /// <param name="propertyName">被检查的属性名。</param>
    /// <param name="length">领域服务里使用的长度上限。</param>
    [Theory]
    [InlineData(nameof(SysAiAssistant), nameof(SysAiAssistant.AssistantCode), 100)]
    [InlineData(nameof(SysAiAssistant), nameof(SysAiAssistant.AssistantName), 100)]
    [InlineData(nameof(SysAiAssistant), nameof(SysAiAssistant.Avatar), 500)]
    [InlineData(nameof(SysAiAssistant), nameof(SysAiAssistant.Description), 500)]
    [InlineData(nameof(SysAiAssistant), nameof(SysAiAssistant.Greeting), 1000)]
    [InlineData(nameof(SysAiPrompt), nameof(SysAiPrompt.PromptCode), 100)]
    [InlineData(nameof(SysAiPrompt), nameof(SysAiPrompt.PromptName), 200)]
    [InlineData(nameof(SysAiPrompt), nameof(SysAiPrompt.Category), 100)]
    [InlineData(nameof(SysAiPrompt), nameof(SysAiPrompt.Version), 100)]
    [InlineData(nameof(SysAiProvider), nameof(SysAiProvider.ConfigCode), 100)]
    [InlineData(nameof(SysAiProvider), nameof(SysAiProvider.ConfigName), 200)]
    [InlineData(nameof(SysAiProvider), nameof(SysAiProvider.Provider), 50)]
    [InlineData(nameof(SysAiProvider), nameof(SysAiProvider.Model), 100)]
    [InlineData(nameof(SysAiProvider), nameof(SysAiProvider.EmbeddingModel), 100)]
    [InlineData(nameof(SysAiProvider), nameof(SysAiProvider.BaseUrl), 500)]
    [InlineData(nameof(SysKnowledgeDocument), nameof(SysKnowledgeDocument.Title), 200)]
    [InlineData(nameof(SysKnowledgeDocument), nameof(SysKnowledgeDocument.Source), 500)]
    [InlineData(nameof(SysKnowledgeDocument), nameof(SysKnowledgeDocument.EmbeddingProviderCode), 100)]
    [InlineData(nameof(SysKnowledgeDocument), nameof(SysKnowledgeDocument.ErrorMessage), 1000)]
    public void Entity_ColumnLength_ShouldMatchDomainValidation(string entityTypeName, string propertyName, int length)
    {
        var entityType = PersistedEntityTypes.Single(type => string.Equals(type.Name, entityTypeName, StringComparison.Ordinal));
        var column = entityType.GetProperty(propertyName)!.GetCustomAttribute<SugarColumn>()!;

        Assert.Equal(length, column.Length);
    }

    /// <summary>
    /// 四张表的备注列必须统一是可空的 500 字，领域服务对备注的上限校验就是按这个数写的。
    /// </summary>
    /// <param name="entityType">被检查的实体类型。</param>
    [Theory]
    [MemberData(nameof(AllEntityTypes))]
    public void Entity_RemarkColumn_ShouldBeNullable500(Type entityType)
    {
        var column = entityType.GetProperty("Remark")!.GetCustomAttribute<SugarColumn>()!;

        Assert.Equal(500, column.Length);
        Assert.True(column.IsNullable, $"{entityType.Name}.Remark 必须可空。");
    }

    /// <summary>
    /// 三张带编码的表，其编码列必须非空——编码是唯一索引的组成部分，可空会让唯一约束对 NULL 行整体失效。
    /// </summary>
    /// <param name="entityTypeName">实体类型名。</param>
    /// <param name="propertyName">编码属性名。</param>
    [Theory]
    [InlineData(nameof(SysAiAssistant), nameof(SysAiAssistant.AssistantCode))]
    [InlineData(nameof(SysAiPrompt), nameof(SysAiPrompt.PromptCode))]
    [InlineData(nameof(SysAiProvider), nameof(SysAiProvider.ConfigCode))]
    public void Entity_BusinessCodeColumn_ShouldBeNonNullable(string entityTypeName, string propertyName)
    {
        var entityType = PersistedEntityTypes.Single(type => string.Equals(type.Name, entityTypeName, StringComparison.Ordinal));
        var column = entityType.GetProperty(propertyName)!.GetCustomAttribute<SugarColumn>()!;

        Assert.False(column.IsNullable, $"{entityTypeName}.{propertyName} 是唯一索引成员，不能可空。");
    }

    /// <summary>
    /// 实体的默认值必须与前端表单默认值一致，否则"新建时不填"与"接口直连不传"会得到两种行为。
    /// </summary>
    [Fact]
    public void Entity_ScalarDefaults_ShouldMatchProductDefaults()
    {
        var assistant = new SysAiAssistant();
        var provider = new SysAiProvider();
        var document = new SysKnowledgeDocument();

        Assert.True(assistant.EnableKnowledge);
        Assert.Equal(5, assistant.KnowledgeTopK);
        Assert.Equal(10, assistant.HistoryRounds);
        Assert.False(assistant.IsDefault);
        Assert.True(assistant.IsEnabled);
        Assert.False(provider.IsDefault);
        Assert.True(provider.IsEnabled);
        Assert.Equal(0, document.ChunkCount);
        Assert.Equal(Domain.Enums.KnowledgeIndexStatus.Pending, document.Status);
    }

    /// <summary>
    /// 断言某张表有且仅有一条唯一索引，并落在指定的业务编码列上。
    /// </summary>
    /// <param name="entityType">实体类型。</param>
    /// <param name="codeProperty">业务编码属性名。</param>
    private static void AssertSingleUniqueIndexOn(Type entityType, string codeProperty)
    {
        var unique = entityType.GetCustomAttributes<SugarIndexAttribute>().Where(index => index.IsUnique).ToList();

        Assert.True(unique.Count == 1, $"{entityType.Name} 的唯一索引应恰好一条，实际 {unique.Count} 条。");
        Assert.True(
            unique[0].IndexFields.Keys.Contains(codeProperty, StringComparer.Ordinal),
            $"{entityType.Name} 的唯一索引 {unique[0].IndexName} 未包含编码列 {codeProperty}。");
    }
}
