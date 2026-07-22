// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 租户版本仓储接口
/// </summary>
public interface ITenantEditionRepository : ISaasRepository<SysTenantEdition>
{
    /// <summary>
    /// 获取默认版本
    /// </summary>
    Task<SysTenantEdition?> GetDefaultEditionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查版本编码是否存在
    /// </summary>
    Task<bool> ExistsCodeAsync(string editionCode, long? excludeId = null, CancellationToken cancellationToken = default);
}
