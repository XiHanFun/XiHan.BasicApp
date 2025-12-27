#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysAccessLogService
// Guid:d1b2c3d4-e5f6-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;

namespace XiHan.BasicApp.Rbac.Services.Abstractions;

/// <summary>
/// 系统访问日志服务接口
/// </summary>
public interface ISysAccessLogService
{
    /// <summary>
    /// 根据ID获取访问日志
    /// </summary>
    Task<SysAccessLog?> GetByIdAsync(XiHanBasicAppIdType id);

    /// <summary>
    /// 获取所有访问日志
    /// </summary>
    Task<List<SysAccessLog>> GetAllAsync();

    /// <summary>
    /// 创建访问日志
    /// </summary>
    Task<SysAccessLog> CreateAsync(SysAccessLog entity);

    /// <summary>
    /// 根据用户ID获取访问日志列表
    /// </summary>
    Task<List<SysAccessLog>> GetByUserIdAsync(XiHanBasicAppIdType userId);

    /// <summary>
    /// 根据资源路径获取访问日志列表
    /// </summary>
    Task<List<SysAccessLog>> GetByResourcePathAsync(string resourcePath);

    /// <summary>
    /// 根据租户ID获取访问日志列表
    /// </summary>
    Task<List<SysAccessLog>> GetByTenantIdAsync(XiHanBasicAppIdType tenantId);

    /// <summary>
    /// 根据时间范围获取访问日志列表
    /// </summary>
    Task<List<SysAccessLog>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime);
}
