#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:KnowledgeRetrieveSkill
// Guid:a11c0de0-7001-4a10-9a00-00000000ai70
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/05 18:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;
using Microsoft.Extensions.AI;
using XiHan.Framework.AI.Abstractions.Rag;
using XiHan.Framework.AI.Abstractions.Skills;

namespace XiHan.BasicApp.AI.Infrastructure.Skills;

/// <summary>
/// 知识检索技能（把 M3 的 <see cref="IKnowledgeRetriever"/> 暴露为对话工具 / MCP tool）
/// </summary>
/// <remarks>
/// 只读、无副作用 → MCP 安全(无需批准)。经 MCP 暴露时无用户/租户上下文(应用管理的 key 为平台级凭据),
/// 故检索不加租户过滤(知识库文档为平台级);topK 内部收敛到 1~20。
/// </remarks>
public sealed class KnowledgeRetrieveSkill : IAiSkill
{
    private const int MaxTopK = 20;
    private const int DefaultTopK = 5;

    private readonly IKnowledgeRetriever _retriever;

    /// <summary>
    /// 构造函数
    /// </summary>
    public KnowledgeRetrieveSkill(IKnowledgeRetriever retriever)
    {
        _retriever = retriever;
    }

    /// <inheritdoc />
    public string Name => "knowledge_retrieve";

    /// <inheritdoc />
    public string Description => "检索 XiHan 知识库，返回与查询最相关的文档片段(带标题/来源/相似度)。回答需要引用知识库内容时调用。";

    /// <inheritdoc />
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
        var chunks = await _retriever.RetrieveAsync(query, effectiveTopK, filter: null, provider: null, cancellationToken);
        return chunks
            .Select(c => new KnowledgeChunkResult(c.DocumentId, c.Title, c.Source, c.Text, c.Score))
            .ToList();
    }

    /// <summary>
    /// 检索片段结果（工具返回，序列化给模型）
    /// </summary>
    private sealed record KnowledgeChunkResult(string DocumentId, string? Title, string? Source, string Text, double? Score);
}
