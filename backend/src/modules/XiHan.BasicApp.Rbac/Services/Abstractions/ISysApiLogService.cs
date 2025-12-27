#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysApiLogService
// Guid:d2b2c3d4-e5f6-7890-abcd-ef1234567891
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;

namespace XiHan.BasicApp.Rbac.Services.Abstractions;

/// <summary>
/// 系统API日志服务接口
/// </summary>
public interface ISysApiLogService
{
    /// <summary>
    /// 根据ID获取API日志
    /// </summary>
    Task<SysApiLog?> GetByIdAsync(XiHanBasicAppIdType id);

    /// <summary>
    /// 获取所有API日志
    /// </summary>
    Task<List<SysApiLog>> GetAllAsync();

    /// <summary>
    /// 创建API日志
    /// </summary>
    Task<SysApiLog> CreateAsync(SysApiLog entity);

    /// <summary>
    /// 根据用户ID获取API日志列表
    /// </summary>
    Task<List<SysApiLog>> GetByUserIdAsync(XiHanBasicAppIdType userId);

    /// <summary>
    /// 根据API路径获取日志列表
    /// </summary>
    Task<List<SysApiLog>> GetByApiPathAsync(string apiPath);

    /// <summary>
    /// 根据租户ID获取API日志列表
    /// </summary>
    Task<List<SysApiLog>> GetByTenantIdAsync(XiHanBasicAppIdType tenantId);

    /// <summary>
    /// 根据时间范围获取API日志列表
    /// </summary>
    Task<List<SysApiLog>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime);

    /// <summary>
    /// 根据状态码获取API日志列表
    /// </summary>
    Task<List<SysApiLog>> GetByStatusCodeAsync(int statusCode);
}
