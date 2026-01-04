#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysAccessLogRepository
// Guid:a1b2c3d4-e5f6-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.AccessLogs;

/// <summary>
/// 系统访问日志仓储接口
/// </summary>
public interface ISysAccessLogRepository : IRepositoryBase<SysAccessLog, long>
{
    /// <summary>
    /// 根据用户ID获取访问日志列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    Task<List<SysAccessLog>> GetByUserIdAsync(long userId);

    /// <summary>
    /// 根据资源路径获取访问日志列表
    /// </summary>
    /// <param name="resourcePath">资源路径</param>
    /// <returns></returns>
    Task<List<SysAccessLog>> GetByResourcePathAsync(string resourcePath);

    /// <summary>
    /// 根据租户ID获取访问日志列表
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns></returns>
    Task<List<SysAccessLog>> GetByTenantIdAsync(long tenantId);

    /// <summary>
    /// 根据时间范围获取访问日志列表
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <returns></returns>
    Task<List<SysAccessLog>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime);
}
