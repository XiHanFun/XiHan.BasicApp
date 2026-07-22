// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 资源仓储接口
/// </summary>
public interface IResourceRepository : ISaasAggregateRepository<SysResource>
{
    /// <summary>
    /// 根据当前租户和资源编码获取资源
    /// </summary>
    Task<SysResource?> GetByCodeAsync(string resourceCode, CancellationToken cancellationToken = default);
}
