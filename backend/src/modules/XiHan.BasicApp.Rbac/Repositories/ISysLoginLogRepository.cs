#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysLoginLogRepository
// Guid:c1c24565-abe6-4377-a349-5c39b4bd1b1e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/11 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories;

/// <summary>
/// 系统登录日志仓储接口
/// </summary>
public interface ISysLoginLogRepository : IReadOnlyRepositoryBase<SysLoginLog, long>
{
    /// <summary>
    /// 根据用户ID获取登录日志
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>登录日志列表</returns>
    Task<List<SysLoginLog>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存登录日志
    /// </summary>
    /// <param name="loginLog">登录日志实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>保存的登录日志实体</returns>
    Task<SysLoginLog> SaveAsync(SysLoginLog loginLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取最近的登录失败记录数
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <param name="minutes">时间范围（分钟）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>失败次数</returns>
    Task<int> GetRecentFailureCountAsync(string userName, int minutes = 30, CancellationToken cancellationToken = default);
}
