#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:KnowledgeQueryAppService
// Guid:a11c0de0-5015-4a10-9a00-00000000ai5e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/05 16:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using XiHan.BasicApp.AI.Application.Contracts;
using XiHan.BasicApp.AI.Application.Dtos;
using XiHan.BasicApp.AI.Application.Mappers;
using XiHan.BasicApp.AI.Domain.Permissions;
using XiHan.BasicApp.AI.Infrastructure.Configuration;
using XiHan.Framework.AI.Abstractions.Chat;
using XiHan.Framework.AI.Abstractions.Rag;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.AI.Application.AppServices;

/// <summary>
/// 知识检索/问答应用服务（RAG 试玩：检索 → 注入 → 会话生成带引用答案）
/// </summary>
[DynamicApi(Group = "BasicApp.AI", GroupName = "AI 服务", Tag = "知识检索")]
public sealed class KnowledgeQueryAppService : AiApplicationService, IKnowledgeQueryAppService
{
    private const int MaxTopK = 20;

    private readonly IKnowledgeRetriever _retriever;
    private readonly IRagPromptAugmenter _augmenter;
    private readonly IXiHanAiService _aiService;
    private readonly ICurrentTenant _currentTenant;
    private readonly XiHanRagOptions _ragOptions;

    /// <summary>
    /// 构造函数
    /// </summary>
    public KnowledgeQueryAppService(
        IKnowledgeRetriever retriever,
        IRagPromptAugmenter augmenter,
        IXiHanAiService aiService,
        ICurrentTenant currentTenant,
        IOptions<XiHanRagOptions> ragOptions)
    {
        _retriever = retriever;
        _augmenter = augmenter;
        _aiService = aiService;
        _currentTenant = currentTenant;
        _ragOptions = ragOptions.Value;
    }

    /// <inheritdoc />
    [PermissionAuthorize(KnowledgePermissionCodes.Execute)]
    [HttpPost]
    public async Task<KnowledgeQueryResultDto> QueryAsync(KnowledgeQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.Query);
        cancellationToken.ThrowIfCancellationRequested();

        var topK = Math.Clamp(input.TopK ?? _ragOptions.DefaultTopK, 1, MaxTopK);

        // 按当前租户过滤（无租户上下文=平台全局 TenantId=0）
        var filter = new RetrievalFilter { TenantId = _currentTenant.Id ?? 0 };
        var retrieved = await _retriever.RetrieveAsync(input.Query.Trim(), topK, filter, input.Provider, cancellationToken);

        var result = new KnowledgeQueryResultDto
        {
            Citations = retrieved.Select(KnowledgeApplicationMapper.ToCitationDto).ToList()
        };

        // 检索到片段且开启问答时，注入 prompt 走会话生成答案
        if (input.Answer && retrieved.Count > 0)
        {
            var augmentedPrompt = _augmenter.Augment(input.Query.Trim(), retrieved);
            var response = await _aiService.ChatAsync(
                [new ChatMessage(ChatRole.User, augmentedPrompt)],
                new XiHanChatOptions { Provider = input.Provider },
                cancellationToken);
            result.Answer = response.Text;
        }

        return result;
    }
}
