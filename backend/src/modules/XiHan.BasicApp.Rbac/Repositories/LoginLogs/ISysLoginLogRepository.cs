#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysLoginLogRepository
// Guid:a3b2c3d4-e5f6-7890-abcd-ef1234567892
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.LoginLogs;

/// <summary>
/// 系统登录日志仓储接口
/// </summary>
public interface ISysLoginLogRepository : IRepositoryBase<SysLoginLog, XiHanBasicAppIdType>
{
    /// <summary>
    /// 根据用户ID获取登录日志列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    Task<List<SysLoginLog>> GetByUserIdAsync(XiHanBasicAppIdType userId);

    /// <summary>
    /// 根据用户名获取登录日志列表
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <returns></returns>
    Task<List<SysLoginLog>> GetByUserNameAsync(string userName);

    /// <summary>
    /// 根据时间范围获取登录日志列表
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <returns></returns>
    Task<List<SysLoginLog>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime);

    /// <summary>
    /// 获取最近的登录日志
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="count">数量</param>
    /// <returns></returns>
    Task<List<SysLoginLog>> GetRecentLoginLogsAsync(XiHanBasicAppIdType userId, int count = 10);
}
