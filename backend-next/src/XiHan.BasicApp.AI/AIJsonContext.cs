// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Serialization;
using XiHan.BasicApp.AI.Operations.AiAssistants;
using XiHan.BasicApp.AI.Operations.AiPrompts;
using XiHan.BasicApp.AI.Operations.AiProviders;
using XiHan.BasicApp.AI.Operations.KnowledgeDocuments;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;

namespace XiHan.BasicApp.AI;

/// <summary>
/// AI 模块的 JSON 源生成上下文
/// </summary>
/// <remarks>
/// 本包全部请求与响应；枚举按名称字符串
/// </remarks>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(AiProviderIdRequest))]
[JsonSerializable(typeof(CreateAiProviderRequest))]
[JsonSerializable(typeof(UpdateAiProviderRequest))]
[JsonSerializable(typeof(SetAiProviderStatusRequest))]
[JsonSerializable(typeof(AiProviderIdsRequest))]
[JsonSerializable(typeof(AiProviderResponse))]
[JsonSerializable(typeof(AiProviderTestResponse))]
[JsonSerializable(typeof(PageRequest))]
[JsonSerializable(typeof(PageResult<AiProviderResponse>))]
[JsonSerializable(typeof(AiPromptIdRequest))]
[JsonSerializable(typeof(CreateAiPromptRequest))]
[JsonSerializable(typeof(UpdateAiPromptRequest))]
[JsonSerializable(typeof(SetAiPromptStatusRequest))]
[JsonSerializable(typeof(AiPromptIdsRequest))]
[JsonSerializable(typeof(AiPromptResponse))]
[JsonSerializable(typeof(AiPromptDetailResponse))]
[JsonSerializable(typeof(PageResult<AiPromptResponse>))]
[JsonSerializable(typeof(AiAssistantIdRequest))]
[JsonSerializable(typeof(CreateAiAssistantRequest))]
[JsonSerializable(typeof(UpdateAiAssistantRequest))]
[JsonSerializable(typeof(SetAiAssistantStatusRequest))]
[JsonSerializable(typeof(AiAssistantIdsRequest))]
[JsonSerializable(typeof(AiAssistantToolsRequest))]
[JsonSerializable(typeof(AiAssistantResponse))]
[JsonSerializable(typeof(AiAssistantToolsResponse))]
[JsonSerializable(typeof(PageResult<AiAssistantResponse>))]
[JsonSerializable(typeof(IReadOnlyList<string>))]
[JsonSerializable(typeof(KnowledgeDocumentIdRequest))]
[JsonSerializable(typeof(IngestKnowledgeDocumentRequest))]
[JsonSerializable(typeof(KnowledgeDocumentIdsRequest))]
[JsonSerializable(typeof(QueryKnowledgeDocumentsRequest))]
[JsonSerializable(typeof(SearchKnowledgeDocumentsRequest))]
[JsonSerializable(typeof(KnowledgeDocumentResponse))]
[JsonSerializable(typeof(KnowledgeDocumentDetailResponse))]
[JsonSerializable(typeof(KnowledgeAnswerResponse))]
[JsonSerializable(typeof(KnowledgeSearchResponse))]
[JsonSerializable(typeof(PageResult<KnowledgeDocumentResponse>))]
[JsonSerializable(typeof(BatchResult))]
public sealed partial class AIJsonContext : JsonSerializerContext;
