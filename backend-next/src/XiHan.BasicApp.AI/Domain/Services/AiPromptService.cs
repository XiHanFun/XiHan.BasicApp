// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.AI.Domain.Services;

/// <summary>
/// 提示词的规则
/// </summary>
/// <remarks>
/// 编码唯一、建后不可改（照 4.x）
/// </remarks>
/// <param name="prompts">提示词仓储</param>
public sealed class AiPromptService(IRepository<SysAiPrompt, Guid> prompts)
{
    /// <summary>
    /// 不存在
    /// </summary>
    /// <param name="id">提示词 ID</param>
    public static Error NotFound(Guid id)
        => Error.NotFound(AIErrors.PromptNotFound, AIMessages.PromptNotFound, MessageArguments.Of(("id", id)));

    /// <summary>
    /// 按 ID 取提示词
    /// </summary>
    /// <param name="id">提示词 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<Result<SysAiPrompt>> FindAsync(Guid id, CancellationToken cancellationToken)
        => (await prompts.FindAsync(id, cancellationToken).ConfigureAwait(false)).TryGetValue(out var prompt) ? prompt : NotFound(id);

    /// <summary>
    /// 编码是否已被别的提示词占用
    /// </summary>
    /// <param name="code">编码</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<bool> CodeTakenAsync(string code, CancellationToken cancellationToken)
        => await prompts.CountAsync(EntityQuery.All<SysAiPrompt>().Where(new Specification<SysAiPrompt>(p => p.Code == code)), cancellationToken).ConfigureAwait(false) > 0;
}
