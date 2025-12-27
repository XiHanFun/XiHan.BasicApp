#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysApiLogService
// Guid:e2b2c3d4-e5f6-7890-abcd-ef1234567891
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories.Abstractions;
using XiHan.BasicApp.Rbac.Services.Abstractions;

namespace XiHan.BasicApp.Rbac.Services.Implementations;

/// <summary>
/// 系统API日志服务实现
/// </summary>
public class SysApiLogService : ISysApiLogService
{
    private readonly ISysApiLogRepository _repository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysApiLogService(ISysApiLogRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 根据ID获取API日志
    /// </summary>
    public async Task<SysApiLog?> GetByIdAsync(XiHanBasicAppIdType id)
    {
        return await _repository.GetByIdAsync(id);
    }

    /// <summary>
    /// 获取所有API日志
    /// </summary>
    public async Task<List<SysApiLog>> GetAllAsync()
    {
        var result = await _repository.GetListAsync();
        return result.ToList();
    }

    /// <summary>
    /// 创建API日志
    /// </summary>
    public async Task<SysApiLog> CreateAsync(SysApiLog entity)
    {
        await _repository.AddAsync(entity);
        return entity;
    }

    /// <summary>
    /// 根据用户ID获取API日志列表
    /// </summary>
    public async Task<List<SysApiLog>> GetByUserIdAsync(XiHanBasicAppIdType userId)
    {
        return await _repository.GetByUserIdAsync(userId);
    }

    /// <summary>
    /// 根据API路径获取日志列表
    /// </summary>
    public async Task<List<SysApiLog>> GetByApiPathAsync(string apiPath)
    {
        return await _repository.GetByApiPathAsync(apiPath);
    }

    /// <summary>
    /// 根据租户ID获取API日志列表
    /// </summary>
    public async Task<List<SysApiLog>> GetByTenantIdAsync(XiHanBasicAppIdType tenantId)
    {
        return await _repository.GetByTenantIdAsync(tenantId);
    }

    /// <summary>
    /// 根据时间范围获取API日志列表
    /// </summary>
    public async Task<List<SysApiLog>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        return await _repository.GetByTimeRangeAsync(startTime, endTime);
    }

    /// <summary>
    /// 根据状态码获取API日志列表
    /// </summary>
    public async Task<List<SysApiLog>> GetByStatusCodeAsync(int statusCode)
    {
        return await _repository.GetByStatusCodeAsync(statusCode);
    }
}
