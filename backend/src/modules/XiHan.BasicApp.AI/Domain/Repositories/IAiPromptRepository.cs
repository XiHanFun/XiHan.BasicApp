// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.AI.Domain.Repositories;

/// <summary>
/// AI 提示词仓储接口
/// </summary>
public interface IAiPromptRepository : ISaasRepository<SysAiPrompt>
{
    /// <summary>
    /// 按编码获取（任意状态，用于详情/唯一性）
    /// </summary>
    Task<SysAiPrompt?> GetByCodeAsync(string promptCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查编码是否存在
    /// </summary>
    Task<bool> ExistsCodeAsync(string promptCode, long? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按编码获取启用记录（库解析路径；version 为空取任一启用）
    /// </summary>
    Task<SysAiPrompt?> GetEnabledByCodeAsync(string promptCode, string? version, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取全部启用提示词
    /// </summary>
    Task<IReadOnlyList<SysAiPrompt>> GetEnabledListAsync(CancellationToken cancellationToken = default);
}
