// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Localization.Abstractions.Messages;

namespace XiHan.BasicApp.AI;

/// <summary>
/// AI 模块的消息键
/// </summary>
/// <remarks>
/// 各区域文案在 <c>Localization/&lt;区域&gt;.json</c>。操作说明用约定键 <c>&lt;操作 ID&gt;.description</c>（资源段的 <c>-</c> 换成 <c>_</c>）
/// 业务消息的主题段用复数资源（<c>ai.providers.*</c>），与框架 AI 子系统的 <c>ai.provider.*</c>、<c>ai.prompt.*</c> 等分开
/// </remarks>
public static class AIMessages
{
    /// <summary>
    /// 消息目录名
    /// </summary>
    public const string CatalogName = "basicapp.ai";

    /// <summary>
    /// 操作 <c>ai-providers.list</c> 的说明
    /// </summary>
    public const string ListAiProvidersDescription = "ai_providers.list.description";

    /// <summary>
    /// 操作 <c>ai-providers.get</c> 的说明
    /// </summary>
    public const string GetAiProviderDescription = "ai_providers.get.description";

    /// <summary>
    /// 操作 <c>ai-providers.create</c> 的说明
    /// </summary>
    public const string CreateAiProviderDescription = "ai_providers.create.description";

    /// <summary>
    /// 操作 <c>ai-providers.update</c> 的说明
    /// </summary>
    public const string UpdateAiProviderDescription = "ai_providers.update.description";

    /// <summary>
    /// 操作 <c>ai-providers.set-status</c> 的说明
    /// </summary>
    public const string SetAiProviderStatusDescription = "ai_providers.set_status.description";

    /// <summary>
    /// 操作 <c>ai-providers.set-default</c> 的说明
    /// </summary>
    public const string SetDefaultAiProviderDescription = "ai_providers.set_default.description";

    /// <summary>
    /// 操作 <c>ai-providers.delete</c> 的说明
    /// </summary>
    public const string DeleteAiProvidersDescription = "ai_providers.delete.description";

    /// <summary>
    /// 操作 <c>ai-providers.test</c> 的说明
    /// </summary>
    public const string TestAiProviderDescription = "ai_providers.test.description";

    /// <summary>
    /// 操作 <c>ai-prompts.list</c> 的说明
    /// </summary>
    public const string ListAiPromptsDescription = "ai_prompts.list.description";

    /// <summary>
    /// 操作 <c>ai-prompts.get</c> 的说明
    /// </summary>
    public const string GetAiPromptDescription = "ai_prompts.get.description";

    /// <summary>
    /// 操作 <c>ai-prompts.create</c> 的说明
    /// </summary>
    public const string CreateAiPromptDescription = "ai_prompts.create.description";

    /// <summary>
    /// 操作 <c>ai-prompts.update</c> 的说明
    /// </summary>
    public const string UpdateAiPromptDescription = "ai_prompts.update.description";

    /// <summary>
    /// 操作 <c>ai-prompts.set-status</c> 的说明
    /// </summary>
    public const string SetAiPromptStatusDescription = "ai_prompts.set_status.description";

    /// <summary>
    /// 操作 <c>ai-prompts.delete</c> 的说明
    /// </summary>
    public const string DeleteAiPromptsDescription = "ai_prompts.delete.description";

    /// <summary>
    /// 操作 <c>ai-assistants.list</c> 的说明
    /// </summary>
    public const string ListAiAssistantsDescription = "ai_assistants.list.description";

    /// <summary>
    /// 操作 <c>ai-assistants.get</c> 的说明
    /// </summary>
    public const string GetAiAssistantDescription = "ai_assistants.get.description";

    /// <summary>
    /// 操作 <c>ai-assistants.create</c> 的说明
    /// </summary>
    public const string CreateAiAssistantDescription = "ai_assistants.create.description";

    /// <summary>
    /// 操作 <c>ai-assistants.update</c> 的说明
    /// </summary>
    public const string UpdateAiAssistantDescription = "ai_assistants.update.description";

    /// <summary>
    /// 操作 <c>ai-assistants.set-status</c> 的说明
    /// </summary>
    public const string SetAiAssistantStatusDescription = "ai_assistants.set_status.description";

    /// <summary>
    /// 操作 <c>ai-assistants.set-default</c> 的说明
    /// </summary>
    public const string SetDefaultAiAssistantDescription = "ai_assistants.set_default.description";

    /// <summary>
    /// 操作 <c>ai-assistants.delete</c> 的说明
    /// </summary>
    public const string DeleteAiAssistantsDescription = "ai_assistants.delete.description";

    /// <summary>
    /// 操作 <c>ai-assistants.tools</c> 的说明
    /// </summary>
    public const string ListAiAssistantToolsDescription = "ai_assistants.tools.description";

    /// <summary>
    /// 操作 <c>knowledge-documents.list</c> 的说明
    /// </summary>
    public const string ListKnowledgeDocumentsDescription = "knowledge_documents.list.description";

    /// <summary>
    /// 操作 <c>knowledge-documents.get</c> 的说明
    /// </summary>
    public const string GetKnowledgeDocumentDescription = "knowledge_documents.get.description";

    /// <summary>
    /// 操作 <c>knowledge-documents.ingest</c> 的说明
    /// </summary>
    public const string IngestKnowledgeDocumentDescription = "knowledge_documents.ingest.description";

    /// <summary>
    /// 操作 <c>knowledge-documents.reindex</c> 的说明
    /// </summary>
    public const string ReindexKnowledgeDocumentDescription = "knowledge_documents.reindex.description";

    /// <summary>
    /// 操作 <c>knowledge-documents.delete</c> 的说明
    /// </summary>
    public const string DeleteKnowledgeDocumentsDescription = "knowledge_documents.delete.description";

    /// <summary>
    /// 操作 <c>knowledge-documents.query</c> 的说明
    /// </summary>
    public const string QueryKnowledgeDocumentsDescription = "knowledge_documents.query.description";

    /// <summary>
    /// 操作 <c>knowledge-documents.search</c> 的说明
    /// </summary>
    public const string SearchKnowledgeDocumentsDescription = "knowledge_documents.search.description";

    /// <summary>
    /// 提供商不存在（参数 <c>id</c>）
    /// </summary>
    public const string ProviderNotFound = "ai.providers.not_found";

    /// <summary>
    /// 已有同编码的提供商（参数 <c>code</c>）
    /// </summary>
    public const string ProviderCodeTaken = "ai.providers.code_taken";

    /// <summary>
    /// 停用的提供商不能设为缺省（参数 <c>code</c>）
    /// </summary>
    public const string ProviderDisabled = "ai.providers.disabled";

    /// <summary>
    /// 端点不是绝对的 http 或 https 地址
    /// </summary>
    public const string ProviderEndpointInvalid = "ai.providers.endpoint_invalid";

    /// <summary>
    /// API 密钥解不开（参数 <c>code</c>）
    /// </summary>
    public const string ProviderSecretUnreadable = "ai.providers.secret_unreadable";

    /// <summary>
    /// 没有 OpenAI 兼容协议的模型 Provider
    /// </summary>
    public const string ProviderKindUnavailable = "ai.providers.kind_unavailable";

    /// <summary>
    /// 提示词不存在（参数 <c>id</c>）
    /// </summary>
    public const string PromptNotFound = "ai.prompts.not_found";

    /// <summary>
    /// 已有同编码的提示词（参数 <c>code</c>）
    /// </summary>
    public const string PromptCodeTaken = "ai.prompts.code_taken";

    /// <summary>
    /// 提供商还被助手或知识文档引用，不能删除（参数 <c>code</c>）
    /// </summary>
    public const string ProviderInUse = "ai.providers.in_use";

    /// <summary>
    /// 有一侧读不出提供商的引用（参数 <c>side</c>）
    /// </summary>
    public const string ProviderReferencesUnavailable = "ai.providers.references_unavailable";

    /// <summary>
    /// 知识文档不存在（参数 <c>id</c>）
    /// </summary>
    public const string KnowledgeDocumentNotFound = "ai.knowledge_documents.not_found";

    /// <summary>
    /// 知识文档指定的嵌入提供商不存在或已停用（参数 <c>code</c>）
    /// </summary>
    public const string KnowledgeProviderUnavailable = "ai.knowledge_documents.provider_unavailable";

    /// <summary>
    /// 知识文档切不出块
    /// </summary>
    public const string KnowledgeNoChunks = "ai.knowledge_documents.no_chunks";

    /// <summary>
    /// 提示词还被助手引用，不能删除（参数 <c>code</c>）
    /// </summary>
    public const string PromptInUse = "ai.prompts.in_use";

    /// <summary>
    /// 助手不存在（参数 <c>id</c>）
    /// </summary>
    public const string AssistantNotFound = "ai.assistants.not_found";

    /// <summary>
    /// 已有同编码的助手（参数 <c>code</c>）
    /// </summary>
    public const string AssistantCodeTaken = "ai.assistants.code_taken";

    /// <summary>
    /// 停用的助手不能设为缺省（参数 <c>code</c>）
    /// </summary>
    public const string AssistantDisabled = "ai.assistants.disabled";

    /// <summary>
    /// 编码不能有空白
    /// </summary>
    public const string AssistantCodeInvalid = "ai.assistants.code_invalid";

    /// <summary>
    /// 引用的提供商不存在或已停用（参数 <c>code</c>）
    /// </summary>
    public const string AssistantProviderUnavailable = "ai.assistants.provider_unavailable";

    /// <summary>
    /// 引用的提示词不存在或已停用（参数 <c>code</c>）
    /// </summary>
    public const string AssistantPromptUnavailable = "ai.assistants.prompt_unavailable";

    /// <summary>
    /// 勾选的工具不在工具目录里（参数 <c>name</c>）
    /// </summary>
    public const string AssistantToolUnavailable = "ai.assistants.tool_unavailable";

    /// <summary>
    /// 回复时引用的提示词不存在或已停用（参数 <c>code</c>）
    /// </summary>
    public const string AssistantPromptDisabled = "ai.assistants.prompt_disabled";

    /// <summary>
    /// 没配提示词时的系统提示词
    /// </summary>
    public const string AssistantDefaultSystemPrompt = "ai.assistants.default_system_prompt";

    /// <summary>
    /// 字段不能全是空白
    /// </summary>
    public const string FieldBlank = "ai.fields.blank";

    /// <summary>
    /// 列表不支持的筛选（参数 <c>field</c>、<c>operator</c>）
    /// </summary>
    public const string FilterUnsupported = "ai.query.filter_unsupported";

    /// <summary>
    /// 列表不支持的排序（参数 <c>field</c>）
    /// </summary>
    public const string SortUnsupported = "ai.query.sort_unsupported";

    /// <summary>
    /// 改了提供商的地址要重新填写密钥
    /// </summary>
    public const string ProviderSecretRequired = "ai.provider.secret_required";

    private static readonly Lazy<MessageCatalog> _catalog = new(static () => MessageCatalog.FromEmbeddedResources(CatalogName, typeof(AIMessages)));

    /// <summary>
    /// 本包的消息目录
    /// </summary>
    public static MessageCatalog Catalog => _catalog.Value;
}
