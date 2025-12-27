#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysAccessLogService
// Guid:e1b2c3d4-e5f6-7890-abcd-ef1234567890
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
/// 系统访问日志服务实现
/// </summary>
public class SysAccessLogService : ISysAccessLogService
{
    private readonly ISysAccessLogRepository _repository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysAccessLogService(ISysAccessLogRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 根据ID获取访问日志
    /// </summary>
    public async Task<SysAccessLog?> GetByIdAsync(XiHanBasicAppIdType id)
    {
        return await _repository.GetByIdAsync(id);
    }

    /// <summary>
    /// 获取所有访问日志
    /// </summary>
    public async Task<List<SysAccessLog>> GetAllAsync()
    {
        var result = await _repository.GetListAsync();
        return result.ToList();
    }

    /// <summary>
    /// 创建访问日志
    /// </summary>
    public async Task<SysAccessLog> CreateAsync(SysAccessLog entity)
    {
        await _repository.AddAsync(entity);
        return entity;
    }

    /// <summary>
    /// 根据用户ID获取访问日志列表
    /// </summary>
    public async Task<List<SysAccessLog>> GetByUserIdAsync(XiHanBasicAppIdType userId)
    {
        return await _repository.GetByUserIdAsync(userId);
    }

    /// <summary>
    /// 根据资源路径获取访问日志列表
    /// </summary>
    public async Task<List<SysAccessLog>> GetByResourcePathAsync(string resourcePath)
    {
        return await _repository.GetByResourcePathAsync(resourcePath);
    }

    /// <summary>
    /// 根据租户ID获取访问日志列表
    /// </summary>
    public async Task<List<SysAccessLog>> GetByTenantIdAsync(XiHanBasicAppIdType tenantId)
    {
        return await _repository.GetByTenantIdAsync(tenantId);
    }

    /// <summary>
    /// 根据时间范围获取访问日志列表
    /// </summary>
    public async Task<List<SysAccessLog>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        return await _repository.GetByTimeRangeAsync(startTime, endTime);
    }
}
