// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.AI.Domain.Repositories;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Infrastructure.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.AI.Infrastructure.Repositories;

/// <summary>
/// AI 提示词仓储实现
/// </summary>
public sealed class AiPromptRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysAiPrompt>(clientResolver), IAiPromptRepository
{
    /// <summary>
    /// 按编码获取（任意状态，用于详情/唯一性）
    /// </summary>
    public async Task<SysAiPrompt?> GetByCodeAsync(string promptCode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(promptCode);
        cancellationToken.ThrowIfCancellationRequested();

        var code = promptCode.Trim();
        return await CreateQueryable()
            .Where(prompt => prompt.PromptCode == code)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 检查编码是否存在
    /// </summary>
    public async Task<bool> ExistsCodeAsync(string promptCode, long? excludeId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(promptCode);
        cancellationToken.ThrowIfCancellationRequested();

        var code = promptCode.Trim();
        var query = CreateQueryable().Where(prompt => prompt.PromptCode == code);
        if (excludeId.HasValue)
        {
            query = query.Where(prompt => prompt.BasicId != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// 按编码获取启用记录（库解析路径；version 为空取任一启用）
    /// </summary>
    public async Task<SysAiPrompt?> GetEnabledByCodeAsync(string promptCode, string? version, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(promptCode);
        cancellationToken.ThrowIfCancellationRequested();

        var code = promptCode.Trim();
        var query = CreateQueryable()
            .Where(prompt => prompt.PromptCode == code && prompt.IsEnabled && prompt.Status == EnableStatus.Enabled);
        if (!string.IsNullOrWhiteSpace(version))
        {
            var trimmedVersion = version.Trim();
            query = query.Where(prompt => prompt.Version == trimmedVersion);
        }

        return await query.OrderBy(prompt => prompt.Sort).FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 获取全部启用提示词
    /// </summary>
    public async Task<IReadOnlyList<SysAiPrompt>> GetEnabledListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(prompt => prompt.IsEnabled && prompt.Status == EnableStatus.Enabled)
            .OrderBy(prompt => prompt.Sort)
            .ToListAsync(cancellationToken);
    }
}
