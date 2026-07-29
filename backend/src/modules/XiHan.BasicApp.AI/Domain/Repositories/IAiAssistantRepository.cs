// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.AI.Domain.Repositories;

/// <summary>
/// AI 助手仓储接口
/// </summary>
public interface IAiAssistantRepository : ISaasRepository<SysAiAssistant>
{
    /// <summary>
    /// 按编码获取（任意状态，用于详情/唯一性）
    /// </summary>
    Task<SysAiAssistant?> GetByCodeAsync(string assistantCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查编码是否存在
    /// </summary>
    Task<bool> ExistsCodeAsync(string assistantCode, long? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取全部启用助手（聊天页助手列表，按 Sort 升序）
    /// </summary>
    Task<IReadOnlyList<SysAiAssistant>> GetEnabledListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 按主键获取启用助手（发起会话/回复路径，禁用即拒绝）
    /// </summary>
    Task<SysAiAssistant?> GetEnabledByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取除 excludeId 外仍被标记为默认的助手（默认单选互斥）
    /// </summary>
    Task<IReadOnlyList<SysAiAssistant>> GetOtherDefaultsAsync(long excludeId, CancellationToken cancellationToken = default);
}
