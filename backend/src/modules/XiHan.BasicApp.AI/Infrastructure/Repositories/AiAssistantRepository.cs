// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.AI.Domain.Repositories;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Infrastructure.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.AI.Infrastructure.Repositories;

/// <summary>
/// AI 助手仓储实现
/// </summary>
public sealed class AiAssistantRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysAiAssistant>(clientResolver), IAiAssistantRepository
{
    /// <summary>
    /// 按编码获取（任意状态，用于详情/唯一性）
    /// </summary>
    public async Task<SysAiAssistant?> GetByCodeAsync(string assistantCode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assistantCode);
        cancellationToken.ThrowIfCancellationRequested();

        var code = assistantCode.Trim();
        return await CreateQueryable()
            .Where(assistant => assistant.AssistantCode == code)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 检查编码是否存在
    /// </summary>
    public async Task<bool> ExistsCodeAsync(string assistantCode, long? excludeId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assistantCode);
        cancellationToken.ThrowIfCancellationRequested();

        var code = assistantCode.Trim();
        var query = CreateQueryable().Where(assistant => assistant.AssistantCode == code);
        if (excludeId.HasValue)
        {
            query = query.Where(assistant => assistant.BasicId != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// 获取全部启用助手（聊天页助手列表，按 Sort 升序）
    /// </summary>
    public async Task<IReadOnlyList<SysAiAssistant>> GetEnabledListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(assistant => assistant.IsEnabled && assistant.Status == EnableStatus.Enabled)
            .OrderBy(assistant => assistant.Sort)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 按主键获取启用助手（发起会话/回复路径，禁用即拒绝）
    /// </summary>
    public async Task<SysAiAssistant?> GetEnabledByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(assistant => assistant.BasicId == id && assistant.IsEnabled && assistant.Status == EnableStatus.Enabled)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 获取除 excludeId 外仍被标记为默认的助手（默认单选互斥）
    /// </summary>
    public async Task<IReadOnlyList<SysAiAssistant>> GetOtherDefaultsAsync(long excludeId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(assistant => assistant.IsDefault && assistant.BasicId != excludeId)
            .ToListAsync(cancellationToken);
    }
}
