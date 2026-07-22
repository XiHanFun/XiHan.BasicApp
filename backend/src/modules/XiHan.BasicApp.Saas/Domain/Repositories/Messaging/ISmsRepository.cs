// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 短信仓储接口
/// </summary>
public interface ISmsRepository : ISaasRepository<SysSms>
{
    /// <summary>
    /// 获取待发送短信
    /// </summary>
    Task<IReadOnlyList<SysSms>> GetPendingSmsAsync(int maxCount, CancellationToken cancellationToken = default);
}
