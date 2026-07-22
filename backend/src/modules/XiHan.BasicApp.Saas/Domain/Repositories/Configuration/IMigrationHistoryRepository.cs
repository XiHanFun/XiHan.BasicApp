// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 迁移历史仓储接口
/// </summary>
public interface IMigrationHistoryRepository : ISaasRepository<SysMigrationHistory>
{
    /// <summary>
    /// 根据版本号获取迁移记录
    /// </summary>
    Task<SysMigrationHistory?> GetByVersionAsync(string version, CancellationToken cancellationToken = default);
}
