#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysLoginLogService
// Guid:e3b2c3d4-e5f6-7890-abcd-ef1234567892
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
/// 系统登录日志服务实现
/// </summary>
public class SysLoginLogService : ISysLoginLogService
{
    private readonly ISysLoginLogRepository _repository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysLoginLogService(ISysLoginLogRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 根据ID获取登录日志
    /// </summary>
    public async Task<SysLoginLog?> GetByIdAsync(XiHanBasicAppIdType id)
    {
        return await _repository.GetByIdAsync(id);
    }

    /// <summary>
    /// 创建登录日志
    /// </summary>
    public async Task<SysLoginLog> CreateAsync(SysLoginLog entity)
    {
        await _repository.AddAsync(entity);
        return entity;
    }

    /// <summary>
    /// 根据用户ID获取登录日志列表
    /// </summary>
    public async Task<List<SysLoginLog>> GetByUserIdAsync(XiHanBasicAppIdType userId)
    {
        return await _repository.GetByUserIdAsync(userId);
    }

    /// <summary>
    /// 根据用户名获取登录日志列表
    /// </summary>
    public async Task<List<SysLoginLog>> GetByUserNameAsync(string userName)
    {
        return await _repository.GetByUserNameAsync(userName);
    }

    /// <summary>
    /// 根据时间范围获取登录日志列表
    /// </summary>
    public async Task<List<SysLoginLog>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        return await _repository.GetByTimeRangeAsync(startTime, endTime);
    }

    /// <summary>
    /// 获取最近的登录日志
    /// </summary>
    public async Task<List<SysLoginLog>> GetRecentLoginLogsAsync(XiHanBasicAppIdType userId, int count = 10)
    {
        return await _repository.GetRecentLoginLogsAsync(userId, count);
    }
}
