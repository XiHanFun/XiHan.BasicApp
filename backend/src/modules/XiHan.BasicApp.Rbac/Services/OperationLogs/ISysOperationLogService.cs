#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysOperationLogService
// Guid:f4c2d3e4-f5a6-7890-abcd-ef1234567901
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Services.OperationLogs.Dtos;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.OperationLogs;

/// <summary>
/// 系统操作日志服务接口
/// </summary>
public interface ISysOperationLogService : ICrudApplicationService<OperationLogDto, XiHanBasicAppIdType, CreateOperationLogDto, CreateOperationLogDto>
{
    /// <summary>
    /// 根据用户ID获取操作日志列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    Task<List<OperationLogDto>> GetByUserIdAsync(XiHanBasicAppIdType userId);

    /// <summary>
    /// 根据操作类型获取日志列表
    /// </summary>
    /// <param name="operationType">操作类型</param>
    /// <returns></returns>
    Task<List<OperationLogDto>> GetByOperationTypeAsync(OperationType operationType);

    /// <summary>
    /// 根据租户ID获取操作日志列表
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns></returns>
    Task<List<OperationLogDto>> GetByTenantIdAsync(XiHanBasicAppIdType tenantId);

    /// <summary>
    /// 根据时间范围获取操作日志列表
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <returns></returns>
    Task<List<OperationLogDto>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime);

    /// <summary>
    /// 根据模块获取操作日志列表
    /// </summary>
    /// <param name="module">模块名称</param>
    /// <returns></returns>
    Task<List<OperationLogDto>> GetByModuleAsync(string module);
}
