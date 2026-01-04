#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysOperationLogService
// Guid:g4c2d3e4-f5a6-7890-abcd-ef1234567905
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Extensions;
using XiHan.BasicApp.Rbac.Repositories.OperationLogs;
using XiHan.BasicApp.Rbac.Services.OAuthTokens.Dtos;
using XiHan.BasicApp.Rbac.Services.OperationLogs.Dtos;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.OperationLogs;

/// <summary>
/// 系统操作日志服务实现
/// </summary>
public class SysOperationLogService : CrudApplicationServiceBase<SysOperationLog, OperationLogDto, XiHanBasicAppIdType, CreateOperationLogDto, CreateOperationLogDto>, ISysOperationLogService
{
    private readonly ISysOperationLogRepository _operationLogRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysOperationLogService(ISysOperationLogRepository operationLogRepository) : base(operationLogRepository)
    {
        _operationLogRepository = operationLogRepository;
    }

    #region 业务特定方法

    /// <summary>
    /// 根据用户ID获取操作日志列表
    /// </summary>
    public async Task<List<OperationLogDto>> GetByUserIdAsync(XiHanBasicAppIdType userId)
    {
        var logs = await _operationLogRepository.GetByUserIdAsync(userId);
        return logs.Adapt<List<OperationLogDto>>();
    }

    /// <summary>
    /// 根据操作类型获取日志列表
    /// </summary>
    public async Task<List<OperationLogDto>> GetByOperationTypeAsync(OperationType operationType)
    {
        var logs = await _operationLogRepository.GetByOperationTypeAsync(operationType);
        return logs.Adapt<List<OperationLogDto>>();
    }

    /// <summary>
    /// 根据租户ID获取操作日志列表
    /// </summary>
    public async Task<List<OperationLogDto>> GetByTenantIdAsync(XiHanBasicAppIdType tenantId)
    {
        var logs = await _operationLogRepository.GetByTenantIdAsync(tenantId);
        return logs.Adapt<List<OperationLogDto>>();
    }

    /// <summary>
    /// 根据时间范围获取操作日志列表
    /// </summary>
    public async Task<List<OperationLogDto>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        var logs = await _operationLogRepository.GetByTimeRangeAsync(startTime, endTime);
        return logs.Adapt<List<OperationLogDto>>();
    }

    /// <summary>
    /// 根据模块获取操作日志列表
    /// </summary>
    public async Task<List<OperationLogDto>> GetByModuleAsync(string module)
    {
        var logs = await _operationLogRepository.GetByModuleAsync(module);
        return logs.Adapt<List<OperationLogDto>>();
    }

    #endregion 业务特定方法
}
