#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasAiPromptStore
// Guid:2df1b77b-fb34-41a3-a142-7ae71541a6cf
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/06 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.AI.Domain.Repositories;
using XiHan.Framework.AI.Abstractions.Prompts;

namespace XiHan.BasicApp.AI.Infrastructure.Configuration;

/// <summary>
/// SaaS AI 提示词库（数据库实现，覆盖框架默认 Options 实现）
/// </summary>
/// <remarks>
/// 读 <c>SysAiPrompt</c>（含租户过滤），映射为框架 <see cref="AiPromptTemplate"/>。提示词非机密，无解密。
/// Singleton 生命周期，Scoped 仓储经 <see cref="IServiceScopeFactory"/> 开作用域解析。
/// </remarks>
public sealed class SaasAiPromptStore : IAiPromptStore
{
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SaasAiPromptStore(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <inheritdoc />
    public async Task<AiPromptTemplate?> GetAsync(string name, string? version = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        SysAiPrompt? prompt;
        await using (var scope = _scopeFactory.CreateAsyncScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IAiPromptRepository>();
            prompt = await repository.GetEnabledByCodeAsync(name, version, cancellationToken);
        }

        return prompt is null ? null : Map(prompt);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AiPromptTemplate>> ListAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SysAiPrompt> prompts;
        await using (var scope = _scopeFactory.CreateAsyncScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IAiPromptRepository>();
            prompts = await repository.GetEnabledListAsync(cancellationToken);
        }

        return prompts.Select(Map).ToList();
    }

    /// <summary>
    /// 实体 → 框架模板（Name 用 PromptCode，Description 用 PromptName）
    /// </summary>
    private static AiPromptTemplate Map(SysAiPrompt prompt)
    {
        return new AiPromptTemplate
        {
            Name = prompt.PromptCode,
            Content = prompt.Content,
            Version = prompt.Version,
            Description = prompt.PromptName
        };
    }
}
