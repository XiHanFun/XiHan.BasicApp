// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel;
using Microsoft.Extensions.AI;
using XiHan.Framework.AI.Abstractions.Rag;
using XiHan.Framework.AI.Abstractions.Skills;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.AI.Infrastructure.Skills;

/// <summary>
/// 知识检索技能（把 M3 的 <see cref="IKnowledgeRetriever"/> 暴露为对话工具 / MCP tool）
/// </summary>
/// <remarks>
/// 只读、无副作用 → MCP 安全(无需批准)。检索只在当前作用域内进行：对话里调用即当前租户的知识，
/// 经 MCP 暴露时无租户上下文(应用管理的 key 为平台级凭据)即平台(0 号租户)的知识;topK 内部收敛到 1~20。
/// </remarks>
public sealed class KnowledgeRetrieveSkill : IAiSkill
{
    private const int MaxTopK = 20;
    private const int DefaultTopK = 5;

    private readonly IKnowledgeRetriever _retriever;

    private readonly ICurrentTenant _currentTenant;

    /// <summary>
    /// 构造函数
    /// </summary>
    public KnowledgeRetrieveSkill(IKnowledgeRetriever retriever, ICurrentTenant currentTenant)
    {
        _retriever = retriever;
        _currentTenant = currentTenant;
    }

    /// <summary>
    /// 技能名（唯一；作工具名 / MCP tool 名）
    /// </summary>
    public string Name => "knowledge_retrieve";

    /// <summary>
    /// 技能说明（供模型理解何时调用）
    /// </summary>
    public string Description => "检索 XiHan 知识库，返回与查询最相关的文档片段(带标题/来源/相似度)。回答需要引用知识库内容时调用。";

    /// <summary>
    /// 转为可供对话工具调用 / MCP 暴露的 <see cref="AIFunction"/>
    /// </summary>
    public AIFunction AsFunction()
    {
        return AIFunctionFactory.Create(
            RetrieveAsync,
            name: Name,
            description: Description);
    }

    private async Task<IReadOnlyList<KnowledgeChunkResult>> RetrieveAsync(
        [Description("检索查询/问题")] string query,
        [Description("返回的最相关片段数(1~20，默认 5)")] int topK = DefaultTopK,
        CancellationToken cancellationToken = default)
    {
        var effectiveTopK = topK <= 0 ? DefaultTopK : Math.Min(topK, MaxTopK);
        // 只检索当前作用域的知识（平台就是 0 号租户）
        var filter = new RetrievalFilter { TenantId = _currentTenant.Id ?? 0 };
        var chunks = await _retriever.RetrieveAsync(query, effectiveTopK, filter, provider: null, cancellationToken);
        return chunks
            .Select(c => new KnowledgeChunkResult(c.DocumentId, c.Title, c.Source, c.Text, c.Score))
            .ToList();
    }

    /// <summary>
    /// 检索片段结果（工具返回，序列化给模型）
    /// </summary>
    private sealed record KnowledgeChunkResult(string DocumentId, string? Title, string? Source, string Text, double? Score);
}
