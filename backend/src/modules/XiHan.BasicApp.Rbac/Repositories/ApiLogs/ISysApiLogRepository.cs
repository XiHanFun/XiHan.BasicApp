#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysApiLogRepository
// Guid:a2b2c3d4-e5f6-7890-abcd-ef1234567891
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.ApiLogs;

/// <summary>
/// 系统API日志仓储接口
/// </summary>
public interface ISysApiLogRepository : IRepositoryBase<SysApiLog, long>
{
    /// <summary>
    /// 根据用户ID获取API日志列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    Task<List<SysApiLog>> GetByUserIdAsync(long userId);

    /// <summary>
    /// 根据API路径获取日志列表
    /// </summary>
    /// <param name="apiPath">API路径</param>
    /// <returns></returns>
    Task<List<SysApiLog>> GetByApiPathAsync(string apiPath);

    /// <summary>
    /// 根据租户ID获取API日志列表
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns></returns>
    Task<List<SysApiLog>> GetByTenantIdAsync(long tenantId);

    /// <summary>
    /// 根据时间范围获取API日志列表
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <returns></returns>
    Task<List<SysApiLog>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime);

    /// <summary>
    /// 根据状态码获取API日志列表
    /// </summary>
    /// <param name="statusCode">状态码</param>
    /// <returns></returns>
    Task<List<SysApiLog>> GetByStatusCodeAsync(int statusCode);
}
