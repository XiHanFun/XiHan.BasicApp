#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IImportHistoryRepository
// Guid:1f7a3d52-8b4c-4e9f-a1d6-5c2e0b9f8741
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/12 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
