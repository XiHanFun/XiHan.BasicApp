// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 操作仓储接口
/// </summary>
public interface IOperationRepository : ISaasAggregateRepository<SysOperation>
{
    /// <summary>
    /// 根据当前租户和操作编码获取操作
    /// </summary>
    Task<SysOperation?> GetByCodeAsync(string operationCode, CancellationToken cancellationToken = default);
}
