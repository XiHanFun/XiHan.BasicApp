// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 权限委托仓储接口
/// </summary>
public interface IPermissionDelegationRepository : ISaasRepository<SysPermissionDelegation>
{
    /// <summary>
    /// 获取被委托人当前有效的权限委派
    /// </summary>
    Task<IReadOnlyList<SysPermissionDelegation>> GetActiveByDelegateeIdAsync(long delegateeUserId, DateTimeOffset now, CancellationToken cancellationToken = default);
}
