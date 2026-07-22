// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 字典项仓储接口
/// </summary>
public interface IDictItemRepository : ISaasRepository<SysDictItem>
{
    /// <summary>
    /// 根据字典ID获取字典项列表
    /// </summary>
    Task<IReadOnlyList<SysDictItem>> GetByDictIdAsync(long dictId, CancellationToken cancellationToken = default);
}
