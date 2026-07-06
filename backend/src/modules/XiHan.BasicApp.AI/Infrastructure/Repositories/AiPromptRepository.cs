#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AiPromptRepository
// Guid:9d91f0e9-af29-4bad-91ec-f33d9a60fa04
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/06 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
    /// <inheritdoc />
    public async Task<SysAiPrompt?> GetByCodeAsync(string promptCode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(promptCode);
        cancellationToken.ThrowIfCancellationRequested();

        var code = promptCode.Trim();
        return await CreateQueryable()
            .Where(prompt => prompt.PromptCode == code)
            .FirstAsync(cancellationToken);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task<IReadOnlyList<SysAiPrompt>> GetEnabledListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(prompt => prompt.IsEnabled && prompt.Status == EnableStatus.Enabled)
            .OrderBy(prompt => prompt.Sort)
            .ToListAsync(cancellationToken);
    }
}
