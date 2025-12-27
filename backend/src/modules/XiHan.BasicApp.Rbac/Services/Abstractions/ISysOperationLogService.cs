#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysOperationLogService
// Guid:d4b2c3d4-e5f6-7890-abcd-ef1234567893
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Services.Abstractions;

/// <summary>
/// 系统操作日志服务接口
/// </summary>
public interface ISysOperationLogService
{
    /// <summary>
    /// 根据ID获取操作日志
    /// </summary>
    Task<SysOperationLog?> GetByIdAsync(XiHanBasicAppIdType id);

    /// <summary>
    /// 创建操作日志
    /// </summary>
    Task<SysOperationLog> CreateAsync(SysOperationLog entity);

    /// <summary>
    /// 根据用户ID获取操作日志列表
    /// </summary>
    Task<List<SysOperationLog>> GetByUserIdAsync(XiHanBasicAppIdType userId);

    /// <summary>
    /// 根据操作类型获取日志列表
    /// </summary>
    Task<List<SysOperationLog>> GetByOperationTypeAsync(OperationType operationType);

    /// <summary>
    /// 根据租户ID获取操作日志列表
    /// </summary>
    Task<List<SysOperationLog>> GetByTenantIdAsync(XiHanBasicAppIdType tenantId);

    /// <summary>
    /// 根据时间范围获取操作日志列表
    /// </summary>
    Task<List<SysOperationLog>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime);

    /// <summary>
    /// 根据模块获取操作日志列表
    /// </summary>
    Task<List<SysOperationLog>> GetByModuleAsync(string module);
}
