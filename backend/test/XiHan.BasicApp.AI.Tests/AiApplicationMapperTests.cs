// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using XiHan.BasicApp.AI.Application.Dtos;
using XiHan.BasicApp.AI.Application.Mappers;
using XiHan.BasicApp.AI.Domain.DomainServices;
using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.AI.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.AI.Abstractions.Rag.Models;

namespace XiHan.BasicApp.AI.Tests;

/// <summary>
/// 四类应用层映射器的往返一致性测试：手写映射最容易"新增字段忘了加一行"，
/// 本文件既逐字段比对取值，也用反射把"命令参数集合 = DTO 属性集合"钉成结构约束；
/// 另外锁死 provider 密钥永不出现在任何读 DTO 上这条安全红线。
/// </summary>
public sealed class AiApplicationMapperTests
{
    /// <summary>
    /// 助手创建 DTO → 命令必须逐字段搬运，一个都不能漏。
    /// </summary>
    [Fact]
    public void AssistantToCreateCommand_ShouldCarryEveryField()
    {
        var input = new AiAssistantCreateDto
        {
            AssistantCode = "assistant-code",
            AssistantName = "知识助手",
            Avatar = "avatar.png",
            Description = "内部知识问答",
            Greeting = "你好",
            PromptCode = "prompt-code",
            ProviderCode = "provider-code",
            EnableKnowledge = false,
            KnowledgeProviderCode = "embed-code",
            KnowledgeTopK = 9,
            HistoryRounds = 11,
            IsDefault = true,
            IsEnabled = false,
            Sort = 3,
            Status = EnableStatus.Disabled,
            Remark = "备注"
        };

        var command = AiAssistantApplicationMapper.ToCreateCommand(input);

        Assert.Equal(input.AssistantCode, command.AssistantCode, StringComparer.Ordinal);
        Assert.Equal(input.AssistantName, command.AssistantName, StringComparer.Ordinal);
        Assert.Equal(input.Avatar, command.Avatar, StringComparer.Ordinal);
        Assert.Equal(input.Description, command.Description, StringComparer.Ordinal);
        Assert.Equal(input.Greeting, command.Greeting, StringComparer.Ordinal);
        Assert.Equal(input.PromptCode, command.PromptCode, StringComparer.Ordinal);
        Assert.Equal(input.ProviderCode, command.ProviderCode, StringComparer.Ordinal);
        Assert.Equal(input.EnableKnowledge, command.EnableKnowledge);
        Assert.Equal(input.KnowledgeProviderCode, command.KnowledgeProviderCode, StringComparer.Ordinal);
        Assert.Equal(input.KnowledgeTopK, command.KnowledgeTopK);
        Assert.Equal(input.HistoryRounds, command.HistoryRounds);
        Assert.Equal(input.IsDefault, command.IsDefault);
        Assert.Equal(input.IsEnabled, command.IsEnabled);
        Assert.Equal(input.Sort, command.Sort);
        Assert.Equal(input.Status, command.Status);
        Assert.Equal(input.Remark, command.Remark, StringComparer.Ordinal);
    }

    /// <summary>
    /// 助手更新 DTO → 命令必须逐字段搬运，并带上主键。
    /// </summary>
    [Fact]
    public void AssistantToUpdateCommand_ShouldCarryEveryFieldIncludingId()
    {
        var input = new AiAssistantUpdateDto
        {
            BasicId = 42,
            AssistantName = "新名称",
            Avatar = "new-avatar.png",
            Description = "新简介",
            Greeting = "新开场白",
            PromptCode = "new-prompt",
            ProviderCode = "new-provider",
            EnableKnowledge = false,
            KnowledgeProviderCode = "new-embed",
            KnowledgeTopK = 8,
            HistoryRounds = 20,
            IsDefault = true,
            IsEnabled = false,
            Sort = 5,
            Remark = "新备注"
        };

        var command = AiAssistantApplicationMapper.ToUpdateCommand(input);

        Assert.Equal(42, command.BasicId);
        Assert.Equal(input.AssistantName, command.AssistantName, StringComparer.Ordinal);
        Assert.Equal(input.Avatar, command.Avatar, StringComparer.Ordinal);
        Assert.Equal(input.Description, command.Description, StringComparer.Ordinal);
        Assert.Equal(input.Greeting, command.Greeting, StringComparer.Ordinal);
        Assert.Equal(input.PromptCode, command.PromptCode, StringComparer.Ordinal);
        Assert.Equal(input.ProviderCode, command.ProviderCode, StringComparer.Ordinal);
        Assert.Equal(input.EnableKnowledge, command.EnableKnowledge);
        Assert.Equal(input.KnowledgeProviderCode, command.KnowledgeProviderCode, StringComparer.Ordinal);
        Assert.Equal(input.KnowledgeTopK, command.KnowledgeTopK);
        Assert.Equal(input.HistoryRounds, command.HistoryRounds);
        Assert.Equal(input.IsDefault, command.IsDefault);
        Assert.Equal(input.IsEnabled, command.IsEnabled);
        Assert.Equal(input.Sort, command.Sort);
        Assert.Equal(input.Remark, command.Remark, StringComparer.Ordinal);
    }

    /// <summary>
    /// 助手状态 DTO → 命令必须搬运主键、状态与备注。
    /// </summary>
    [Fact]
    public void AssistantToStatusCommand_ShouldCarryIdStatusAndRemark()
    {
        var command = AiAssistantApplicationMapper.ToStatusCommand(new AiAssistantStatusUpdateDto
        {
            BasicId = 42,
            Status = EnableStatus.Disabled,
            Remark = "停用"
        });

        Assert.Equal(42, command.BasicId);
        Assert.Equal(EnableStatus.Disabled, command.Status);
        Assert.Equal("停用", command.Remark, StringComparer.Ordinal);
    }

    /// <summary>
    /// 助手实体 → 列表项必须搬齐列表所需字段；开场白与备注属详情字段，不进列表载荷。
    /// </summary>
    [Fact]
    public void AssistantToListItemDto_ShouldCarryListFieldsOnly()
    {
        var entity = CreateAuditedAssistant();

        var dto = AiAssistantApplicationMapper.ToListItemDto(entity);

        Assert.Equal(entity.BasicId, dto.BasicId);
        Assert.Equal(entity.AssistantCode, dto.AssistantCode, StringComparer.Ordinal);
        Assert.Equal(entity.AssistantName, dto.AssistantName, StringComparer.Ordinal);
        Assert.Equal(entity.Avatar, dto.Avatar, StringComparer.Ordinal);
        Assert.Equal(entity.Description, dto.Description, StringComparer.Ordinal);
        Assert.Equal(entity.PromptCode, dto.PromptCode, StringComparer.Ordinal);
        Assert.Equal(entity.ProviderCode, dto.ProviderCode, StringComparer.Ordinal);
        Assert.Equal(entity.EnableKnowledge, dto.EnableKnowledge);
        Assert.Equal(entity.KnowledgeProviderCode, dto.KnowledgeProviderCode, StringComparer.Ordinal);
        Assert.Equal(entity.KnowledgeTopK, dto.KnowledgeTopK);
        Assert.Equal(entity.HistoryRounds, dto.HistoryRounds);
        Assert.Equal(entity.IsDefault, dto.IsDefault);
        Assert.Equal(entity.IsEnabled, dto.IsEnabled);
        Assert.Equal(entity.Sort, dto.Sort);
        Assert.Equal(entity.Status, dto.Status);
        Assert.Equal(entity.CreatedTime, dto.CreatedTime);
        Assert.Equal(entity.ModifiedTime, dto.ModifiedTime);
        Assert.Equal(typeof(AiAssistantListItemDto), dto.GetType());
    }

    /// <summary>
    /// 助手实体 → 详情必须在列表字段之上补齐开场白、备注与四个审计人字段。
    /// </summary>
    [Fact]
    public void AssistantToDetailDto_ShouldAddGreetingRemarkAndAuditFields()
    {
        var entity = CreateAuditedAssistant();

        var dto = AiAssistantApplicationMapper.ToDetailDto(entity);

        Assert.Equal(entity.AssistantCode, dto.AssistantCode, StringComparer.Ordinal);
        Assert.Equal(entity.Greeting, dto.Greeting, StringComparer.Ordinal);
        Assert.Equal(entity.Remark, dto.Remark, StringComparer.Ordinal);
        Assert.Equal(entity.CreatedId, dto.CreatedId);
        Assert.Equal(entity.CreatedBy, dto.CreatedBy, StringComparer.Ordinal);
        Assert.Equal(entity.ModifiedId, dto.ModifiedId);
        Assert.Equal(entity.ModifiedBy, dto.ModifiedBy, StringComparer.Ordinal);
    }

    /// <summary>
    /// 可用助手 DTO 的主键字段显式命名为 AssistantId，且只暴露聊天页展示所需的六个字段。
    /// </summary>
    [Fact]
    public void AssistantToOptionDto_ShouldExposeChatFacingFieldsUnderAssistantId()
    {
        var entity = CreateAuditedAssistant();

        var dto = AiAssistantApplicationMapper.ToOptionDto(entity);

        Assert.Equal(entity.BasicId, dto.AssistantId);
        Assert.Equal(entity.AssistantCode, dto.AssistantCode, StringComparer.Ordinal);
        Assert.Equal(entity.AssistantName, dto.AssistantName, StringComparer.Ordinal);
        Assert.Equal(entity.Avatar, dto.Avatar, StringComparer.Ordinal);
        Assert.Equal(entity.Description, dto.Description, StringComparer.Ordinal);
        Assert.Equal(entity.IsDefault, dto.IsDefault);
        Assert.Equal(
            ["AssistantCode", "AssistantId", "AssistantName", "Avatar", "Description", "IsDefault"],
            PublicPropertyNames(typeof(AiAssistantOptionDto)),
            StringComparer.Ordinal);
    }

    /// <summary>
    /// 提示词创建/更新/状态映射必须逐字段搬运。
    /// </summary>
    [Fact]
    public void PromptMappers_ShouldCarryEveryField()
    {
        var create = AiPromptApplicationMapper.ToCreateCommand(new AiPromptCreateDto
        {
            PromptCode = "prompt-code",
            PromptName = "通用问答提示词",
            Category = "通用",
            Version = "v1",
            Content = "你是助手。",
            IsEnabled = false,
            Sort = 4,
            Status = EnableStatus.Disabled,
            Remark = "备注"
        });
        var update = AiPromptApplicationMapper.ToUpdateCommand(new AiPromptUpdateDto
        {
            BasicId = 42,
            PromptName = "新名",
            Category = "新分类",
            Version = "v2",
            Content = "新正文。",
            IsEnabled = true,
            Sort = 6,
            Remark = "新备注"
        });
        var status = AiPromptApplicationMapper.ToStatusCommand(new AiPromptStatusUpdateDto
        {
            BasicId = 42,
            Status = EnableStatus.Disabled,
            Remark = "停用"
        });

        Assert.Equal("prompt-code", create.PromptCode, StringComparer.Ordinal);
        Assert.Equal("通用问答提示词", create.PromptName, StringComparer.Ordinal);
        Assert.Equal("通用", create.Category, StringComparer.Ordinal);
        Assert.Equal("v1", create.Version, StringComparer.Ordinal);
        Assert.Equal("你是助手。", create.Content, StringComparer.Ordinal);
        Assert.False(create.IsEnabled);
        Assert.Equal(4, create.Sort);
        Assert.Equal(EnableStatus.Disabled, create.Status);
        Assert.Equal("备注", create.Remark, StringComparer.Ordinal);

        Assert.Equal(42, update.BasicId);
        Assert.Equal("新名", update.PromptName, StringComparer.Ordinal);
        Assert.Equal("新分类", update.Category, StringComparer.Ordinal);
        Assert.Equal("v2", update.Version, StringComparer.Ordinal);
        Assert.Equal("新正文。", update.Content, StringComparer.Ordinal);
        Assert.True(update.IsEnabled);
        Assert.Equal(6, update.Sort);
        Assert.Equal("新备注", update.Remark, StringComparer.Ordinal);

        Assert.Equal(42, status.BasicId);
        Assert.Equal(EnableStatus.Disabled, status.Status);
        Assert.Equal("停用", status.Remark, StringComparer.Ordinal);
    }

    /// <summary>
    /// 提示词列表项必须搬齐列表字段，且正文不进列表载荷（正文可达数万字，列表带上会拖垮分页）。
    /// </summary>
    [Fact]
    public void PromptToListItemDto_ShouldNotCarryContent()
    {
        var entity = CreateAuditedPrompt();

        var dto = AiPromptApplicationMapper.ToListItemDto(entity);

        Assert.Equal(entity.BasicId, dto.BasicId);
        Assert.Equal(entity.PromptCode, dto.PromptCode, StringComparer.Ordinal);
        Assert.Equal(entity.PromptName, dto.PromptName, StringComparer.Ordinal);
        Assert.Equal(entity.Category, dto.Category, StringComparer.Ordinal);
        Assert.Equal(entity.Version, dto.Version, StringComparer.Ordinal);
        Assert.Equal(entity.IsEnabled, dto.IsEnabled);
        Assert.Equal(entity.Sort, dto.Sort);
        Assert.Equal(entity.Status, dto.Status);
        Assert.Equal(entity.CreatedTime, dto.CreatedTime);
        Assert.Equal(entity.ModifiedTime, dto.ModifiedTime);
        Assert.DoesNotContain("Content", PublicPropertyNames(typeof(AiPromptListItemDto)), StringComparer.Ordinal);
    }

    /// <summary>
    /// 提示词详情必须在列表字段之上补齐正文、备注与四个审计人字段。
    /// </summary>
    [Fact]
    public void PromptToDetailDto_ShouldAddContentRemarkAndAuditFields()
    {
        var entity = CreateAuditedPrompt();

        var dto = AiPromptApplicationMapper.ToDetailDto(entity);

        Assert.Equal(entity.PromptCode, dto.PromptCode, StringComparer.Ordinal);
        Assert.Equal(entity.Content, dto.Content, StringComparer.Ordinal);
        Assert.Equal(entity.Remark, dto.Remark, StringComparer.Ordinal);
        Assert.Equal(entity.CreatedId, dto.CreatedId);
        Assert.Equal(entity.CreatedBy, dto.CreatedBy, StringComparer.Ordinal);
        Assert.Equal(entity.ModifiedId, dto.ModifiedId);
        Assert.Equal(entity.ModifiedBy, dto.ModifiedBy, StringComparer.Ordinal);
    }

    /// <summary>
    /// provider 创建/更新/状态映射必须逐字段搬运，含明文密钥（写入方向密钥必须原样透传给领域层加密）。
    /// </summary>
    [Fact]
    public void ProviderMappers_ShouldCarryEveryFieldIncludingWriteOnlyApiKey()
    {
        var create = AiProviderApplicationMapper.ToCreateCommand(new AiProviderCreateDto
        {
            ConfigCode = "config-code",
            ConfigName = "默认配置",
            Provider = "OpenAI",
            Model = "gpt-4o-mini",
            EmbeddingModel = "text-embedding-3-small",
            BaseUrl = "https://api.example.com",
            ApiKey = "sk-plain-key",
            MaxOutputTokens = 1024,
            Temperature = 0.7f,
            TimeoutSeconds = 30,
            ExtraJson = "{\"a\":1}",
            IsDefault = true,
            IsEnabled = false,
            Sort = 2,
            Status = EnableStatus.Disabled,
            Remark = "备注"
        });
        var update = AiProviderApplicationMapper.ToUpdateCommand(new AiProviderUpdateDto
        {
            BasicId = 42,
            ConfigName = "新配置名",
            Provider = "DeepSeek",
            Model = "deepseek-chat",
            EmbeddingModel = "bge-m3",
            BaseUrl = "https://api.new.com",
            ApiKey = null,
            MaxOutputTokens = 2048,
            Temperature = 1.2f,
            TimeoutSeconds = 60,
            ExtraJson = "{\"b\":2}",
            IsDefault = false,
            IsEnabled = true,
            Sort = 5,
            Remark = "新备注"
        });
        var status = AiProviderApplicationMapper.ToStatusCommand(new AiProviderStatusUpdateDto
        {
            BasicId = 42,
            Status = EnableStatus.Disabled,
            Remark = "停用"
        });

        Assert.Equal("config-code", create.ConfigCode, StringComparer.Ordinal);
        Assert.Equal("默认配置", create.ConfigName, StringComparer.Ordinal);
        Assert.Equal("OpenAI", create.Provider, StringComparer.Ordinal);
        Assert.Equal("gpt-4o-mini", create.Model, StringComparer.Ordinal);
        Assert.Equal("text-embedding-3-small", create.EmbeddingModel, StringComparer.Ordinal);
        Assert.Equal("https://api.example.com", create.BaseUrl, StringComparer.Ordinal);
        Assert.Equal("sk-plain-key", create.ApiKey, StringComparer.Ordinal);
        Assert.Equal(1024, create.MaxOutputTokens);
        Assert.Equal(0.7f, create.Temperature);
        Assert.Equal(30, create.TimeoutSeconds);
        Assert.Equal("{\"a\":1}", create.ExtraJson, StringComparer.Ordinal);
        Assert.True(create.IsDefault);
        Assert.False(create.IsEnabled);
        Assert.Equal(2, create.Sort);
        Assert.Equal(EnableStatus.Disabled, create.Status);
        Assert.Equal("备注", create.Remark, StringComparer.Ordinal);

        Assert.Equal(42, update.BasicId);
        Assert.Equal("新配置名", update.ConfigName, StringComparer.Ordinal);
        Assert.Equal("DeepSeek", update.Provider, StringComparer.Ordinal);
        Assert.Equal("deepseek-chat", update.Model, StringComparer.Ordinal);
        Assert.Equal("bge-m3", update.EmbeddingModel, StringComparer.Ordinal);
        Assert.Equal("https://api.new.com", update.BaseUrl, StringComparer.Ordinal);
        Assert.Null(update.ApiKey);
        Assert.Equal(2048, update.MaxOutputTokens);
        Assert.Equal(1.2f, update.Temperature);
        Assert.Equal(60, update.TimeoutSeconds);
        Assert.Equal("{\"b\":2}", update.ExtraJson, StringComparer.Ordinal);
        Assert.False(update.IsDefault);
        Assert.True(update.IsEnabled);
        Assert.Equal(5, update.Sort);
        Assert.Equal("新备注", update.Remark, StringComparer.Ordinal);

        Assert.Equal(42, status.BasicId);
        Assert.Equal(EnableStatus.Disabled, status.Status);
        Assert.Equal("停用", status.Remark, StringComparer.Ordinal);
    }

    /// <summary>
    /// provider 读 DTO 必须只暴露"是否已配置密钥"的布尔标志，密钥字段本身不得存在于任何读 DTO 上。
    /// </summary>
    /// <remarks>这是密钥不外泄的最后一道结构性防线：多一个 ApiKey 属性就意味着密文可能被序列化给前端。</remarks>
    [Fact]
    public void ProviderReadDtos_ShouldNeverExposeApiKey()
    {
        var listNames = PublicPropertyNames(typeof(AiProviderListItemDto));
        var detailNames = PublicPropertyNames(typeof(AiProviderDetailDto));

        Assert.DoesNotContain("ApiKey", listNames, StringComparer.Ordinal);
        Assert.DoesNotContain("ApiKey", detailNames, StringComparer.Ordinal);
        Assert.Contains("HasApiKey", listNames, StringComparer.Ordinal);
        Assert.Contains("HasApiKey", detailNames, StringComparer.Ordinal);
    }

    /// <summary>
    /// HasApiKey 必须严格反映密文是否为空，不能凭 IsEnabled 之类的旁证推断。
    /// </summary>
    /// <param name="apiKey">实体上的密钥列取值。</param>
    /// <param name="expected">期望的 HasApiKey。</param>
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("dp:cipher", true)]
    [InlineData(" ", true)]
    public void ProviderToListItemDto_HasApiKeyShouldReflectStoredCipher(string? apiKey, bool expected)
    {
        var entity = AiTestHelper.CreateProvider(7);
        entity.ApiKey = apiKey;

        Assert.Equal(expected, AiProviderApplicationMapper.ToListItemDto(entity).HasApiKey);
        Assert.Equal(expected, AiProviderApplicationMapper.ToDetailDto(entity).HasApiKey);
    }

    /// <summary>
    /// provider 列表项必须搬齐列表字段；扩展 JSON 与备注属详情字段，不进列表载荷。
    /// </summary>
    [Fact]
    public void ProviderToListItemDto_ShouldCarryListFieldsOnly()
    {
        var entity = CreateAuditedProvider();

        var dto = AiProviderApplicationMapper.ToListItemDto(entity);

        Assert.Equal(entity.BasicId, dto.BasicId);
        Assert.Equal(entity.ConfigCode, dto.ConfigCode, StringComparer.Ordinal);
        Assert.Equal(entity.ConfigName, dto.ConfigName, StringComparer.Ordinal);
        Assert.Equal(entity.Provider, dto.Provider, StringComparer.Ordinal);
        Assert.Equal(entity.Model, dto.Model, StringComparer.Ordinal);
        Assert.Equal(entity.EmbeddingModel, dto.EmbeddingModel, StringComparer.Ordinal);
        Assert.Equal(entity.BaseUrl, dto.BaseUrl, StringComparer.Ordinal);
        Assert.Equal(entity.MaxOutputTokens, dto.MaxOutputTokens);
        Assert.Equal(entity.Temperature, dto.Temperature);
        Assert.Equal(entity.TimeoutSeconds, dto.TimeoutSeconds);
        Assert.Equal(entity.IsDefault, dto.IsDefault);
        Assert.Equal(entity.IsEnabled, dto.IsEnabled);
        Assert.Equal(entity.Sort, dto.Sort);
        Assert.Equal(entity.Status, dto.Status);
        Assert.Equal(entity.CreatedTime, dto.CreatedTime);
        Assert.Equal(entity.ModifiedTime, dto.ModifiedTime);
        Assert.DoesNotContain("ExtraJson", PublicPropertyNames(typeof(AiProviderListItemDto)), StringComparer.Ordinal);
    }

    /// <summary>
    /// provider 详情必须在列表字段之上补齐扩展 JSON、备注与四个审计人字段。
    /// </summary>
    [Fact]
    public void ProviderToDetailDto_ShouldAddExtraJsonRemarkAndAuditFields()
    {
        var entity = CreateAuditedProvider();

        var dto = AiProviderApplicationMapper.ToDetailDto(entity);

        Assert.Equal(entity.ConfigCode, dto.ConfigCode, StringComparer.Ordinal);
        Assert.Equal(entity.ExtraJson, dto.ExtraJson, StringComparer.Ordinal);
        Assert.Equal(entity.Remark, dto.Remark, StringComparer.Ordinal);
        Assert.Equal(entity.CreatedId, dto.CreatedId);
        Assert.Equal(entity.CreatedBy, dto.CreatedBy, StringComparer.Ordinal);
        Assert.Equal(entity.ModifiedId, dto.ModifiedId);
        Assert.Equal(entity.ModifiedBy, dto.ModifiedBy, StringComparer.Ordinal);
    }

    /// <summary>
    /// 连接测试结果映射：会话探测必搬，嵌入探测有则搬含维度、无则保持 null。
    /// </summary>
    [Fact]
    public void ProviderToTestResultDto_ShouldCarryBothProbes()
    {
        var result = new AiProviderTestResult(
            new AiProviderChatProbe(true, "Stop", 120, "gpt-4o-mini"),
            new AiProviderEmbeddingProbe(true, null, 80, "bge-m3", 1024));

        var dto = AiProviderApplicationMapper.ToTestResultDto(result);

        Assert.True(dto.Success);
        Assert.True(dto.Chat.Success);
        Assert.Equal("Stop", dto.Chat.Message, StringComparer.Ordinal);
        Assert.Equal(120, dto.Chat.LatencyMs);
        Assert.Equal("gpt-4o-mini", dto.Chat.Model, StringComparer.Ordinal);
        Assert.Null(dto.Chat.Dimensions);
        Assert.NotNull(dto.Embedding);
        Assert.True(dto.Embedding!.Success);
        Assert.Equal(80, dto.Embedding.LatencyMs);
        Assert.Equal("bge-m3", dto.Embedding.Model, StringComparer.Ordinal);
        Assert.Equal(1024, dto.Embedding.Dimensions);
    }

    /// <summary>
    /// 未配置嵌入模型时结果 DTO 的 Embedding 必须保持 null，前端据此隐藏嵌入探测行。
    /// </summary>
    [Fact]
    public void ProviderToTestResultDto_WithoutEmbeddingShouldKeepNull()
    {
        var result = new AiProviderTestResult(new AiProviderChatProbe(false, "端点不可达", 3000, "gpt-4o-mini"), null);

        var dto = AiProviderApplicationMapper.ToTestResultDto(result);

        Assert.False(dto.Success);
        Assert.Null(dto.Embedding);
        Assert.Equal("端点不可达", dto.Chat.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 知识摄取 DTO → 命令必须逐字段搬运。
    /// </summary>
    [Fact]
    public void KnowledgeToIngestCommand_ShouldCarryEveryField()
    {
        var command = KnowledgeApplicationMapper.ToIngestCommand(new KnowledgeIngestDto
        {
            Title = "运维手册",
            SourceType = KnowledgeSourceType.UploadFile,
            Source = "manual.md",
            Text = "第一章：部署。",
            EmbeddingProviderCode = "embed-code",
            Remark = "备注"
        });

        Assert.Equal("运维手册", command.Title, StringComparer.Ordinal);
        Assert.Equal(KnowledgeSourceType.UploadFile, command.SourceType);
        Assert.Equal("manual.md", command.Source, StringComparer.Ordinal);
        Assert.Equal("第一章：部署。", command.Text, StringComparer.Ordinal);
        Assert.Equal("embed-code", command.EmbeddingProviderCode, StringComparer.Ordinal);
        Assert.Equal("备注", command.Remark, StringComparer.Ordinal);
    }

    /// <summary>
    /// 知识文档列表项必须带上失败原因（前端要在列表直接看到为什么索引失败），但不带原文。
    /// </summary>
    [Fact]
    public void KnowledgeToListItemDto_ShouldCarryErrorMessageButNotRawContent()
    {
        var entity = CreateAuditedDocument();

        var dto = KnowledgeApplicationMapper.ToListItemDto(entity);

        Assert.Equal(entity.BasicId, dto.BasicId);
        Assert.Equal(entity.Title, dto.Title, StringComparer.Ordinal);
        Assert.Equal(entity.SourceType, dto.SourceType);
        Assert.Equal(entity.Source, dto.Source, StringComparer.Ordinal);
        Assert.Equal(entity.ChunkCount, dto.ChunkCount);
        Assert.Equal(entity.EmbeddingProviderCode, dto.EmbeddingProviderCode, StringComparer.Ordinal);
        Assert.Equal(entity.Status, dto.Status);
        Assert.Equal(entity.ErrorMessage, dto.ErrorMessage, StringComparer.Ordinal);
        Assert.Equal(entity.Sort, dto.Sort);
        Assert.Equal(entity.CreatedTime, dto.CreatedTime);
        Assert.Equal(entity.ModifiedTime, dto.ModifiedTime);
        Assert.DoesNotContain("RawContent", PublicPropertyNames(typeof(KnowledgeListItemDto)), StringComparer.Ordinal);
    }

    /// <summary>
    /// 知识文档详情必须在列表字段之上补齐原文、备注与四个审计人字段。
    /// </summary>
    [Fact]
    public void KnowledgeToDetailDto_ShouldAddRawContentRemarkAndAuditFields()
    {
        var entity = CreateAuditedDocument();

        var dto = KnowledgeApplicationMapper.ToDetailDto(entity);

        Assert.Equal(entity.Title, dto.Title, StringComparer.Ordinal);
        Assert.Equal(entity.RawContent, dto.RawContent, StringComparer.Ordinal);
        Assert.Equal(entity.Remark, dto.Remark, StringComparer.Ordinal);
        Assert.Equal(entity.CreatedId, dto.CreatedId);
        Assert.Equal(entity.CreatedBy, dto.CreatedBy, StringComparer.Ordinal);
        Assert.Equal(entity.ModifiedId, dto.ModifiedId);
        Assert.Equal(entity.ModifiedBy, dto.ModifiedBy, StringComparer.Ordinal);
    }

    /// <summary>
    /// 检索片段 → 引用 DTO 必须搬齐溯源三件套（文档 id、切片序号、来源）与分数、正文。
    /// </summary>
    [Fact]
    public void KnowledgeToCitationDto_ShouldCarryTraceabilityFields()
    {
        var chunk = new RetrievedChunk
        {
            DocumentId = "4242",
            Index = 3,
            Text = "部署步骤如下。",
            Title = "运维手册",
            Source = "manual.md",
            Score = 0.87
        };

        var dto = KnowledgeApplicationMapper.ToCitationDto(chunk);

        Assert.Equal("4242", dto.DocumentId, StringComparer.Ordinal);
        Assert.Equal(3, dto.Index);
        Assert.Equal("部署步骤如下。", dto.Text, StringComparer.Ordinal);
        Assert.Equal("运维手册", dto.Title, StringComparer.Ordinal);
        Assert.Equal("manual.md", dto.Source, StringComparer.Ordinal);
        Assert.Equal(0.87, dto.Score);
    }

    /// <summary>
    /// 命令 record 的位置参数集合必须与对应写 DTO 的可写属性集合完全一致：
    /// DTO 加了字段而映射器忘了搬（或反之）都会让本用例立刻变红。
    /// </summary>
    /// <param name="recordType">命令 record 类型。</param>
    /// <param name="dtoType">对应写 DTO 类型。</param>
    [Theory]
    [InlineData(typeof(AiAssistantCreateCommand), typeof(AiAssistantCreateDto))]
    [InlineData(typeof(AiAssistantUpdateCommand), typeof(AiAssistantUpdateDto))]
    [InlineData(typeof(AiAssistantStatusChangeCommand), typeof(AiAssistantStatusUpdateDto))]
    [InlineData(typeof(AiPromptCreateCommand), typeof(AiPromptCreateDto))]
    [InlineData(typeof(AiPromptUpdateCommand), typeof(AiPromptUpdateDto))]
    [InlineData(typeof(AiPromptStatusChangeCommand), typeof(AiPromptStatusUpdateDto))]
    [InlineData(typeof(AiProviderCreateCommand), typeof(AiProviderCreateDto))]
    [InlineData(typeof(AiProviderUpdateCommand), typeof(AiProviderUpdateDto))]
    [InlineData(typeof(AiProviderStatusChangeCommand), typeof(AiProviderStatusUpdateDto))]
    [InlineData(typeof(KnowledgeIngestCommand), typeof(KnowledgeIngestDto))]
    public void CommandRecords_ShouldMatchWriteDtoFieldSet(Type recordType, Type dtoType)
    {
        AiTestHelper.AssertRecordMatchesDto(recordType, dtoType);
    }

    /// <summary>
    /// 全部映射器方法都必须对 null 入参 fail-fast，绝不允许把半空对象喂进领域层。
    /// </summary>
    [Fact]
    public void AllMappers_NullInputShouldThrowArgumentNullException()
    {
        _ = Assert.Throws<ArgumentNullException>(() => AiAssistantApplicationMapper.ToCreateCommand(null!));
        _ = Assert.Throws<ArgumentNullException>(() => AiAssistantApplicationMapper.ToUpdateCommand(null!));
        _ = Assert.Throws<ArgumentNullException>(() => AiAssistantApplicationMapper.ToStatusCommand(null!));
        _ = Assert.Throws<ArgumentNullException>(() => AiAssistantApplicationMapper.ToListItemDto(null!));
        _ = Assert.Throws<ArgumentNullException>(() => AiAssistantApplicationMapper.ToDetailDto(null!));
        _ = Assert.Throws<ArgumentNullException>(() => AiAssistantApplicationMapper.ToOptionDto(null!));
        _ = Assert.Throws<ArgumentNullException>(() => AiPromptApplicationMapper.ToCreateCommand(null!));
        _ = Assert.Throws<ArgumentNullException>(() => AiPromptApplicationMapper.ToUpdateCommand(null!));
        _ = Assert.Throws<ArgumentNullException>(() => AiPromptApplicationMapper.ToStatusCommand(null!));
        _ = Assert.Throws<ArgumentNullException>(() => AiPromptApplicationMapper.ToListItemDto(null!));
        _ = Assert.Throws<ArgumentNullException>(() => AiPromptApplicationMapper.ToDetailDto(null!));
        _ = Assert.Throws<ArgumentNullException>(() => AiProviderApplicationMapper.ToCreateCommand(null!));
        _ = Assert.Throws<ArgumentNullException>(() => AiProviderApplicationMapper.ToUpdateCommand(null!));
        _ = Assert.Throws<ArgumentNullException>(() => AiProviderApplicationMapper.ToStatusCommand(null!));
        _ = Assert.Throws<ArgumentNullException>(() => AiProviderApplicationMapper.ToListItemDto(null!));
        _ = Assert.Throws<ArgumentNullException>(() => AiProviderApplicationMapper.ToDetailDto(null!));
        _ = Assert.Throws<ArgumentNullException>(() => AiProviderApplicationMapper.ToTestResultDto(null!));
        _ = Assert.Throws<ArgumentNullException>(() => KnowledgeApplicationMapper.ToIngestCommand(null!));
        _ = Assert.Throws<ArgumentNullException>(() => KnowledgeApplicationMapper.ToListItemDto(null!));
        _ = Assert.Throws<ArgumentNullException>(() => KnowledgeApplicationMapper.ToDetailDto(null!));
        _ = Assert.Throws<ArgumentNullException>(() => KnowledgeApplicationMapper.ToCitationDto(null!));
    }

    /// <summary>
    /// 取类型的公共实例属性名（按序数排序，便于集合比对）。
    /// </summary>
    /// <param name="type">目标类型。</param>
    /// <returns>属性名集合。</returns>
    private static IReadOnlyList<string> PublicPropertyNames(Type type)
    {
        return [.. type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)];
    }

    /// <summary>
    /// 构造带审计信息的助手实体。
    /// </summary>
    /// <returns>助手实体。</returns>
    private static SysAiAssistant CreateAuditedAssistant()
    {
        var entity = AiTestHelper.CreateAssistant(42);
        entity.CreatedTime = DateTimeOffset.UnixEpoch;
        entity.ModifiedTime = DateTimeOffset.UnixEpoch.AddDays(1);
        entity.CreatedId = 11;
        entity.CreatedBy = "creator";
        entity.ModifiedId = 12;
        entity.ModifiedBy = "modifier";
        return entity;
    }

    /// <summary>
    /// 构造带审计信息的提示词实体。
    /// </summary>
    /// <returns>提示词实体。</returns>
    private static SysAiPrompt CreateAuditedPrompt()
    {
        var entity = AiTestHelper.CreatePrompt(42);
        entity.CreatedTime = DateTimeOffset.UnixEpoch;
        entity.ModifiedTime = DateTimeOffset.UnixEpoch.AddDays(1);
        entity.CreatedId = 11;
        entity.CreatedBy = "creator";
        entity.ModifiedId = 12;
        entity.ModifiedBy = "modifier";
        return entity;
    }

    /// <summary>
    /// 构造带审计信息的 provider 实体。
    /// </summary>
    /// <returns>provider 实体。</returns>
    private static SysAiProvider CreateAuditedProvider()
    {
        var entity = AiTestHelper.CreateProvider(42);
        entity.CreatedTime = DateTimeOffset.UnixEpoch;
        entity.ModifiedTime = DateTimeOffset.UnixEpoch.AddDays(1);
        entity.CreatedId = 11;
        entity.CreatedBy = "creator";
        entity.ModifiedId = 12;
        entity.ModifiedBy = "modifier";
        return entity;
    }

    /// <summary>
    /// 构造带审计信息与失败原因的知识文档实体。
    /// </summary>
    /// <returns>知识文档实体。</returns>
    private static SysKnowledgeDocument CreateAuditedDocument()
    {
        var entity = AiTestHelper.CreateDocument(42, chunkCount: 5);
        entity.ErrorMessage = "上一轮失败原因";
        entity.CreatedTime = DateTimeOffset.UnixEpoch;
        entity.ModifiedTime = DateTimeOffset.UnixEpoch.AddDays(1);
        entity.CreatedId = 11;
        entity.CreatedBy = "creator";
        entity.ModifiedId = 12;
        entity.ModifiedBy = "modifier";
        return entity;
    }
}
