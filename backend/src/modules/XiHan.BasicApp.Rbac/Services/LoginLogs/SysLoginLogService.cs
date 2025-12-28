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

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Extensions;
using XiHan.BasicApp.Rbac.Repositories.LoginLogs;
using XiHan.BasicApp.Rbac.Services.LoginLogs.Dtos;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.LoginLogs;

/// <summary>
/// 系统登录日志服务实现
/// </summary>
public class SysLoginLogService : CrudApplicationServiceBase<SysLoginLog, LoginLogDto, XiHanBasicAppIdType, CreateLoginLogDto, CreateLoginLogDto>, ISysLoginLogService
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
    public async Task<List<LoginLogDto>> GetByUserIdAsync(XiHanBasicAppIdType userId)
    {
        var logs = await _loginLogRepository.GetByUserIdAsync(userId);
        return logs.ToDto();
    }

    /// <summary>
    /// 根据用户名获取登录日志列表
    /// </summary>
    public async Task<List<LoginLogDto>> GetByUserNameAsync(string userName)
    {
        var logs = await _loginLogRepository.GetByUserNameAsync(userName);
        return logs.ToDto();
    }

    /// <summary>
    /// 根据时间范围获取登录日志列表
    /// </summary>
    public async Task<List<LoginLogDto>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        var logs = await _loginLogRepository.GetByTimeRangeAsync(startTime, endTime);
        return logs.ToDto();
    }

    /// <summary>
    /// 获取最近的登录日志
    /// </summary>
    public async Task<List<LoginLogDto>> GetRecentLoginLogsAsync(XiHanBasicAppIdType userId, int count = 10)
    {
        var logs = await _loginLogRepository.GetRecentLoginLogsAsync(userId, count);
        return logs.ToDto();
    }

    #endregion 业务特定方法

    #region 映射方法实现

    /// <summary>
    /// 映射实体到DTO
    /// </summary>
    protected override Task<LoginLogDto> MapToEntityDtoAsync(SysLoginLog entity)
    {
        return Task.FromResult(entity.ToDto());
    }

    /// <summary>
    /// 映射 CreateLoginLogDto 到实体
    /// </summary>
    protected override Task<SysLoginLog> MapToEntityAsync(CreateLoginLogDto createDto)
    {
        var entity = new SysLoginLog
        {
            UserId = createDto.UserId,
            UserName = createDto.UserName,
            LoginIp = createDto.LoginIp,
            LoginLocation = createDto.LoginLocation,
            Browser = createDto.Browser,
            Os = createDto.Os,
            Status = createDto.Status,
            Message = createDto.Message
        };

        return Task.FromResult(entity);
    }

    protected override Task MapToEntityAsync(CreateLoginLogDto updateDto, SysLoginLog entity)
    {
        throw new NotImplementedException();
    }

    protected override Task<SysLoginLog> MapToEntityAsync(LoginLogDto dto)
    {
        throw new NotImplementedException();
    }

    protected override Task MapToEntityAsync(LoginLogDto dto, SysLoginLog entity)
    {
        throw new NotImplementedException();
    }

    #endregion 映射方法实现
}
