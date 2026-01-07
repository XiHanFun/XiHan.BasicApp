#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ILoginLogRepository
// Guid:c5d6e7f8-a9b0-4c5d-1e2f-4a5b6c7d8e9f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// 登录日志仓储接口
/// </summary>
public interface ILoginLogRepository : IRepositoryBase<SysLoginLog, long>
{
    /// <summary>
    /// 根据用户ID获取登录日志
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>登录日志列表</returns>
    Task<List<SysLoginLog>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据登录状态获取登录日志
    /// </summary>
    /// <param name="loginResult">登录状态</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>登录日志列表</returns>
    Task<List<SysLoginLog>> GetByLoginResultAsync(LoginResult loginResult, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户最后一次成功登录记录
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>登录日志</returns>
    Task<SysLoginLog?> GetLastSuccessLoginAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户连续登录失败次数
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>失败次数</returns>
    Task<int> GetContinuousFailureCountAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定IP的登录记录
    /// </summary>
    /// <param name="ipAddress">IP地址</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>登录日志列表</returns>
    Task<List<SysLoginLog>> GetByIpAddressAsync(string ipAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定时间段内的登录日志
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>登录日志列表</returns>
    Task<List<SysLoginLog>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清理指定时间之前的日志
    /// </summary>
    /// <param name="beforeTime">截止时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>清理数量</returns>
    Task<int> CleanLogsBeforeAsync(DateTimeOffset beforeTime, CancellationToken cancellationToken = default);
}
