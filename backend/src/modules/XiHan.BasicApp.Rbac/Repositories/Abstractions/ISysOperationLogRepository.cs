#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysOperationLogRepository
// Guid:a4b2c3d4-e5f6-7890-abcd-ef1234567893
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstractions;

/// <summary>
/// 系统操作日志仓储接口
/// </summary>
public interface ISysOperationLogRepository : IRepositoryBase<SysOperationLog, XiHanBasicAppIdType>
{
    /// <summary>
    /// 根据用户ID获取操作日志列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    Task<List<SysOperationLog>> GetByUserIdAsync(XiHanBasicAppIdType userId);

    /// <summary>
    /// 根据操作类型获取日志列表
    /// </summary>
    /// <param name="operationType">操作类型</param>
    /// <returns></returns>
    Task<List<SysOperationLog>> GetByOperationTypeAsync(OperationType operationType);

    /// <summary>
    /// 根据租户ID获取操作日志列表
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns></returns>
    Task<List<SysOperationLog>> GetByTenantIdAsync(XiHanBasicAppIdType tenantId);

    /// <summary>
    /// 根据时间范围获取操作日志列表
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <returns></returns>
    Task<List<SysOperationLog>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime);

    /// <summary>
    /// 根据模块获取操作日志列表
    /// </summary>
    /// <param name="module">模块名称</param>
    /// <returns></returns>
    Task<List<SysOperationLog>> GetByModuleAsync(string module);
}
