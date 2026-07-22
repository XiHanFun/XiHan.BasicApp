// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 版本仓储接口
/// </summary>
public interface IVersionRepository : ISaasRepository<SysVersion>
{
    /// <summary>
    /// 获取最新版本
    /// </summary>
    Task<SysVersion?> GetLatestAsync(CancellationToken cancellationToken = default);
}
