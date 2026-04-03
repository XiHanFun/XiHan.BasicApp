#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ILoginLogRepository
// Guid:d12cb41d-2619-4fff-9edc-e7cb6899c819
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:35:44
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 登录日志仓储接口
/// </summary>
public interface ILoginLogRepository : IRepositoryBase<SysLoginLog, long>
{
    /// <summary>
    /// 获取用户最近失败登录次数
    /// </summary>
    Task<int> GetRecentFailureCountAsync(string userName, int minutes, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按用户ID分页查询登录日志
    /// </summary>
    Task<(List<SysLoginLog> Items, int Total)> GetPagedByUserIdAsync(
        long userId, long? tenantId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
}
