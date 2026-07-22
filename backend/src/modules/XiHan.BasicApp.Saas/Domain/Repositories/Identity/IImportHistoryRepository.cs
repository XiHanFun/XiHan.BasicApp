// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 导入历史仓储接口
/// </summary>
public interface IImportHistoryRepository : ISaasRepository<SysImportHistory>
{
    /// <summary>
    /// 获取用户在指定页面最近的导入记录（按创建时间倒序）
    /// </summary>
    Task<List<SysImportHistory>> GetRecentByUserAsync(long userId, string pageCode, int count, CancellationToken cancellationToken = default);
}
