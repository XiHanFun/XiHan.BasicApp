// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 角色仓储接口
/// </summary>
public interface IRoleRepository : ISaasAggregateRepository<SysRole>
{
    /// <summary>
    /// 根据当前租户和角色编码获取角色
    /// </summary>
    Task<SysRole?> GetByCodeAsync(string roleCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取有效角色集合
    /// </summary>
    Task<IReadOnlyList<SysRole>> GetEnabledByIdsAsync(IEnumerable<long> roleIds, CancellationToken cancellationToken = default);
}
