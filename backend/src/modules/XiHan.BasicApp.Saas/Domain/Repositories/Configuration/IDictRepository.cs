// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 字典仓储接口
/// </summary>
public interface IDictRepository : ISaasRepository<SysDict>
{
    /// <summary>
    /// 根据字典编码获取
    /// </summary>
    Task<SysDict?> GetByCodeAsync(string dictCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查字典编码是否存在
    /// </summary>
    Task<bool> ExistsCodeAsync(string dictCode, long? excludeId = null, CancellationToken cancellationToken = default);
}
