#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysLoginLogService
// Guid:d3b2c3d4-e5f6-7890-abcd-ef1234567892
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;

namespace XiHan.BasicApp.Rbac.Services.Abstractions;

/// <summary>
/// 系统登录日志服务接口
/// </summary>
public interface ISysLoginLogService
{
    /// <summary>
    /// 根据ID获取登录日志
    /// </summary>
    Task<SysLoginLog?> GetByIdAsync(XiHanBasicAppIdType id);

    /// <summary>
    /// 创建登录日志
    /// </summary>
    Task<SysLoginLog> CreateAsync(SysLoginLog entity);

    /// <summary>
    /// 根据用户ID获取登录日志列表
    /// </summary>
    Task<List<SysLoginLog>> GetByUserIdAsync(XiHanBasicAppIdType userId);

    /// <summary>
    /// 根据用户名获取登录日志列表
    /// </summary>
    Task<List<SysLoginLog>> GetByUserNameAsync(string userName);

    /// <summary>
    /// 根据时间范围获取登录日志列表
    /// </summary>
    Task<List<SysLoginLog>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime);

    /// <summary>
    /// 获取最近的登录日志
    /// </summary>
    Task<List<SysLoginLog>> GetRecentLoginLogsAsync(XiHanBasicAppIdType userId, int count = 10);
}
