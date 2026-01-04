#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysLoginLogService
// Guid:j2c2d3e4-f5a6-7890-abcd-ef1234567912
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories.LoginLogs;
using XiHan.BasicApp.Rbac.Services.LoginLogs.Dtos;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.LoginLogs;

/// <summary>
/// 系统登录日志服务实现
/// </summary>
public class SysLoginLogService : CrudApplicationServiceBase<SysLoginLog, LoginLogDto, long, CreateLoginLogDto, CreateLoginLogDto>, ISysLoginLogService
{
    private readonly ISysLoginLogRepository _loginLogRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysLoginLogService(ISysLoginLogRepository loginLogRepository) : base(loginLogRepository)
    {
        _loginLogRepository = loginLogRepository;
    }

    #region 业务特定方法

    /// <summary>
    /// 根据用户ID获取登录日志列表
    /// </summary>
    public async Task<List<LoginLogDto>> GetByUserIdAsync(long userId)
    {
        var logs = await _loginLogRepository.GetByUserIdAsync(userId);
        return logs.Adapt<List<LoginLogDto>>();
    }

    /// <summary>
    /// 根据用户名获取登录日志列表
    /// </summary>
    public async Task<List<LoginLogDto>> GetByUserNameAsync(string userName)
    {
        var logs = await _loginLogRepository.GetByUserNameAsync(userName);
        return logs.Adapt<List<LoginLogDto>>();
    }

    /// <summary>
    /// 根据时间范围获取登录日志列表
    /// </summary>
    public async Task<List<LoginLogDto>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        var logs = await _loginLogRepository.GetByTimeRangeAsync(startTime, endTime);
        return logs.Adapt<List<LoginLogDto>>();
    }

    /// <summary>
    /// 获取最近的登录日志
    /// </summary>
    public async Task<List<LoginLogDto>> GetRecentLoginLogsAsync(long userId, int count = 10)
    {
        var logs = await _loginLogRepository.GetRecentLoginLogsAsync(userId, count);
        return logs.Adapt<List<LoginLogDto>>();
    }

    #endregion 业务特定方法
}
