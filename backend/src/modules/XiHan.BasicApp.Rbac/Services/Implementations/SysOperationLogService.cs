#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysOperationLogService
// Guid:e4b2c3d4-e5f6-7890-abcd-ef1234567893
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Repositories.Abstractions;
using XiHan.BasicApp.Rbac.Services.Abstractions;

namespace XiHan.BasicApp.Rbac.Services.Implementations;

/// <summary>
/// 系统操作日志服务实现
/// </summary>
public class SysOperationLogService : ISysOperationLogService
{
    private readonly ISysOperationLogRepository _repository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysOperationLogService(ISysOperationLogRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 根据ID获取操作日志
    /// </summary>
    public async Task<SysOperationLog?> GetByIdAsync(XiHanBasicAppIdType id)
    {
        return await _repository.GetByIdAsync(id);
    }

    /// <summary>
    /// 创建操作日志
    /// </summary>
    public async Task<SysOperationLog> CreateAsync(SysOperationLog entity)
    {
        await _repository.AddAsync(entity);
        return entity;
    }

    /// <summary>
    /// 根据用户ID获取操作日志列表
    /// </summary>
    public async Task<List<SysOperationLog>> GetByUserIdAsync(XiHanBasicAppIdType userId)
    {
        return await _repository.GetByUserIdAsync(userId);
    }

    /// <summary>
    /// 根据操作类型获取日志列表
    /// </summary>
    public async Task<List<SysOperationLog>> GetByOperationTypeAsync(OperationType operationType)
    {
        return await _repository.GetByOperationTypeAsync(operationType);
    }

    /// <summary>
    /// 根据租户ID获取操作日志列表
    /// </summary>
    public async Task<List<SysOperationLog>> GetByTenantIdAsync(XiHanBasicAppIdType tenantId)
    {
        return await _repository.GetByTenantIdAsync(tenantId);
    }

    /// <summary>
    /// 根据时间范围获取操作日志列表
    /// </summary>
    public async Task<List<SysOperationLog>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        return await _repository.GetByTimeRangeAsync(startTime, endTime);
    }

    /// <summary>
    /// 根据模块获取操作日志列表
    /// </summary>
    public async Task<List<SysOperationLog>> GetByModuleAsync(string module)
    {
        return await _repository.GetByModuleAsync(module);
    }
}
