// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using XiHan.BasicApp.AI.Domain.DomainServices;
using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.AI.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.AI.Tests;

/// <summary>
/// 测试夹具共享工具：主键回填、四类实体与命令的构造工厂、反射字段对齐断言器。
/// </summary>
/// <remarks>本文件不含用例，只提供各测试类共用的纯内存构造与反射工具，任何方法都不触发外部 I/O。</remarks>
public static class AiTestHelper
{
    /// <summary>
    /// 测试中模拟持久化层回填受保护的实体主键。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <param name="entity">待回填主键的实体。</param>
    /// <param name="id">主键值。</param>
    /// <returns>回填主键后的同一实体实例。</returns>
    public static TEntity SetBasicId<TEntity>(TEntity entity, long id)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var property = typeof(TEntity).GetProperty(
            "BasicId",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"未找到 {typeof(TEntity).Name} 的主键属性。");
        property.SetValue(entity, id);
        return entity;
    }

    /// <summary>
    /// 测试中回填多租户实体的租户主键。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <param name="entity">待回填的实体。</param>
    /// <param name="tenantId">租户主键。</param>
    /// <returns>回填后的同一实体实例。</returns>
    public static TEntity SetTenantId<TEntity>(TEntity entity, long tenantId)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var property = typeof(TEntity).GetProperty(
            "TenantId",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"未找到 {typeof(TEntity).Name} 的租户属性。");
        property.SetValue(entity, tenantId);
        return entity;
    }

    /// <summary>
    /// 构造一条各字段均合法的助手创建命令，用例按需 with 覆盖单个字段。
    /// </summary>
    /// <returns>助手创建命令。</returns>
    public static AiAssistantCreateCommand CreateAssistantCommand()
    {
        return new AiAssistantCreateCommand(
            "assistant-code",
            "知识助手",
            "avatar.png",
            "内部知识问答",
            "你好",
            "prompt-code",
            "provider-code",
            true,
            "embed-code",
            5,
            10,
            false,
            true,
            0,
            EnableStatus.Enabled,
            "备注");
    }

    /// <summary>
    /// 构造一条各字段均合法的助手更新命令。
    /// </summary>
    /// <param name="basicId">目标助手主键。</param>
    /// <returns>助手更新命令。</returns>
    public static AiAssistantUpdateCommand UpdateAssistantCommand(long basicId = 1)
    {
        return new AiAssistantUpdateCommand(
            basicId,
            "新名称",
            "new-avatar.png",
            "新简介",
            "新开场白",
            "new-prompt",
            "new-provider",
            false,
            "new-embed",
            8,
            20,
            false,
            true,
            3,
            "新备注");
    }

    /// <summary>
    /// 构造一个各字段均合法的助手实体。
    /// </summary>
    /// <param name="basicId">主键。</param>
    /// <param name="code">助手编码。</param>
    /// <returns>助手实体。</returns>
    public static SysAiAssistant CreateAssistant(long basicId = 1, string code = "assistant-code")
    {
        return SetBasicId(new SysAiAssistant
        {
            AssistantCode = code,
            AssistantName = "原名称",
            Avatar = "old-avatar.png",
            Description = "原简介",
            Greeting = "原开场白",
            PromptCode = "old-prompt",
            ProviderCode = "old-provider",
            EnableKnowledge = true,
            KnowledgeProviderCode = "old-embed",
            KnowledgeTopK = 5,
            HistoryRounds = 10,
            IsDefault = false,
            IsEnabled = true,
            Sort = 0,
            Status = EnableStatus.Enabled,
            Remark = "原备注"
        }, basicId);
    }

    /// <summary>
    /// 构造一条各字段均合法的提示词创建命令。
    /// </summary>
    /// <returns>提示词创建命令。</returns>
    public static AiPromptCreateCommand CreatePromptCommand()
    {
        return new AiPromptCreateCommand(
            "prompt-code",
            "通用问答提示词",
            "通用",
            "v1",
            "你是助手。",
            true,
            0,
            EnableStatus.Enabled,
            "备注");
    }

    /// <summary>
    /// 构造一条各字段均合法的提示词更新命令。
    /// </summary>
    /// <param name="basicId">目标提示词主键。</param>
    /// <returns>提示词更新命令。</returns>
    public static AiPromptUpdateCommand UpdatePromptCommand(long basicId = 1)
    {
        return new AiPromptUpdateCommand(
            basicId,
            "新提示词名",
            "新分类",
            "v2",
            "新正文。",
            false,
            5,
            "新备注");
    }

    /// <summary>
    /// 构造一个各字段均合法的提示词实体。
    /// </summary>
    /// <param name="basicId">主键。</param>
    /// <param name="code">提示词编码。</param>
    /// <returns>提示词实体。</returns>
    public static SysAiPrompt CreatePrompt(long basicId = 1, string code = "prompt-code")
    {
        return SetBasicId(new SysAiPrompt
        {
            PromptCode = code,
            PromptName = "原提示词名",
            Category = "原分类",
            Version = "v1",
            Content = "原正文。",
            IsEnabled = true,
            Sort = 0,
            Status = EnableStatus.Enabled,
            Remark = "原备注"
        }, basicId);
    }

    /// <summary>
    /// 构造一条各字段均合法的 provider 创建命令。
    /// </summary>
    /// <returns>provider 创建命令。</returns>
    public static AiProviderCreateCommand CreateProviderCommand()
    {
        return new AiProviderCreateCommand(
            "config-code",
            "默认配置",
            "OpenAI",
            "gpt-4o-mini",
            "text-embedding-3-small",
            "https://api.example.com",
            "sk-plain-key",
            1024,
            0.7f,
            30,
            "{\"a\":1}",
            false,
            true,
            0,
            EnableStatus.Enabled,
            "备注");
    }

    /// <summary>
    /// 构造一条各字段均合法的 provider 更新命令（ApiKey 默认留空表示保留原密钥）。
    /// </summary>
    /// <param name="basicId">目标 provider 主键。</param>
    /// <returns>provider 更新命令。</returns>
    public static AiProviderUpdateCommand UpdateProviderCommand(long basicId = 1)
    {
        return new AiProviderUpdateCommand(
            basicId,
            "新配置名",
            "DeepSeek",
            "deepseek-chat",
            "bge-m3",
            "https://api.new.com",
            null,
            2048,
            1.2f,
            60,
            "{\"b\":2}",
            false,
            true,
            2,
            "新备注");
    }

    /// <summary>
    /// 构造一个各字段均合法的 provider 实体。
    /// </summary>
    /// <param name="basicId">主键。</param>
    /// <param name="code">配置编码。</param>
    /// <returns>provider 实体。</returns>
    public static SysAiProvider CreateProvider(long basicId = 1, string code = "config-code")
    {
        return SetBasicId(new SysAiProvider
        {
            ConfigCode = code,
            ConfigName = "原配置名",
            Provider = "OpenAI",
            Model = "gpt-4o-mini",
            EmbeddingModel = "text-embedding-3-small",
            BaseUrl = "https://api.old.com",
            ApiKey = "dp:old-cipher",
            MaxOutputTokens = 512,
            Temperature = 0.5f,
            TimeoutSeconds = 15,
            ExtraJson = "{\"old\":true}",
            IsDefault = false,
            IsEnabled = true,
            Sort = 0,
            Status = EnableStatus.Enabled,
            Remark = "原备注"
        }, basicId);
    }

    /// <summary>
    /// 构造一条各字段均合法的知识文档摄取命令。
    /// </summary>
    /// <returns>摄取命令。</returns>
    public static KnowledgeIngestCommand CreateIngestCommand()
    {
        return new KnowledgeIngestCommand(
            "运维手册",
            KnowledgeSourceType.PasteText,
            "manual.md",
            "第一章：部署。",
            "embed-code",
            "备注");
    }

    /// <summary>
    /// 构造一个各字段均合法的知识文档实体。
    /// </summary>
    /// <param name="basicId">主键。</param>
    /// <param name="chunkCount">已入库切片数。</param>
    /// <returns>知识文档实体。</returns>
    public static SysKnowledgeDocument CreateDocument(long basicId = 1, int chunkCount = 3)
    {
        return SetBasicId(new SysKnowledgeDocument
        {
            Title = "运维手册",
            SourceType = KnowledgeSourceType.PasteText,
            Source = "manual.md",
            RawContent = "第一章：部署。",
            ChunkCount = chunkCount,
            EmbeddingProviderCode = "embed-code",
            Status = KnowledgeIndexStatus.Indexed,
            Sort = 0,
            Remark = "备注"
        }, basicId);
    }

    /// <summary>
    /// 断言 record 的位置参数名集合与目标类型的公共可写属性名集合完全一致（手写映射不漏字段的强约束）。
    /// </summary>
    /// <param name="recordType">record 命令类型。</param>
    /// <param name="dtoType">对应的 DTO 类型。</param>
    /// <param name="ignoredRecordParameters">允许 record 独有的参数名。</param>
    /// <param name="ignoredDtoProperties">允许 DTO 独有的属性名。</param>
    public static void AssertRecordMatchesDto(
        Type recordType,
        Type dtoType,
        IEnumerable<string>? ignoredRecordParameters = null,
        IEnumerable<string>? ignoredDtoProperties = null)
    {
        ArgumentNullException.ThrowIfNull(recordType);
        ArgumentNullException.ThrowIfNull(dtoType);

        var recordNames = GetRecordParameterNames(recordType)
            .Except(ignoredRecordParameters ?? [], StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();
        var dtoNames = dtoType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.CanWrite)
            .Select(property => property.Name)
            .Except(ignoredDtoProperties ?? [], StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(dtoNames, recordNames);
    }

    /// <summary>
    /// 取 record 主构造函数的位置参数名（按声明顺序）。
    /// </summary>
    /// <param name="recordType">record 类型。</param>
    /// <returns>位置参数名集合。</returns>
    public static IReadOnlyList<string> GetRecordParameterNames(Type recordType)
    {
        ArgumentNullException.ThrowIfNull(recordType);

        var constructor = recordType
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public)
            .OrderByDescending(item => item.GetParameters().Length)
            .First();
        return [.. constructor.GetParameters().Select(parameter => parameter.Name!)];
    }
}
