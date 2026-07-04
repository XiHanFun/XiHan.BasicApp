#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IAiProviderRepository
// Guid:a11c0de0-1006-4a10-9a00-00000000ai15
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/05 14:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.AI.Domain.Repositories;

/// <summary>
/// AI Provider 仓储接口
/// </summary>
public interface IAiProviderRepository : ISaasRepository<SysAiProvider>
{
    /// <summary>
    /// 按配置编码获取（任意状态，用于详情/唯一性）
    /// </summary>
    Task<SysAiProvider?> GetByCodeAsync(string configCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查配置编码是否存在
    /// </summary>
    Task<bool> ExistsCodeAsync(string configCode, long? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按配置编码获取启用记录（配置源运行路径）
    /// </summary>
    Task<SysAiProvider?> GetEnabledByCodeAsync(string configCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取默认且启用的 provider（配置源默认解析）
    /// </summary>
    Task<SysAiProvider?> GetDefaultAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取全部启用 provider（配置源枚举）
    /// </summary>
    Task<IReadOnlyList<SysAiProvider>> GetEnabledListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取其它标记为默认的 provider（用于单默认互斥清理）
    /// </summary>
    Task<IReadOnlyList<SysAiProvider>> GetOtherDefaultsAsync(long keepId, CancellationToken cancellationToken = default);
}
