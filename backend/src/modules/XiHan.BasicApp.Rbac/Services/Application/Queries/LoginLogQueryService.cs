#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:LoginLogQueryService
// Guid:b4c5d6e7-f8a9-4b0c-1d2e-3f4a5b6c7d8e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Domain.Paging.Dtos;

namespace XiHan.BasicApp.Rbac.Services.Application.Queries;

/// <summary>
/// 登录日志查询服务（处理登录日志的读操作）
/// </summary>
public class LoginLogQueryService : ApplicationServiceBase
{
    private readonly ILoginLogRepository _loginLogRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public LoginLogQueryService(ILoginLogRepository loginLogRepository)
    {
        _loginLogRepository = loginLogRepository;
    }

    /// <summary>
    /// 根据ID获取登录日志
    /// </summary>
    public async Task<RbacDtoBase?> GetByIdAsync(long id)
    {
        var log = await _loginLogRepository.GetByIdAsync(id);
        return log?.Adapt<RbacDtoBase>();
    }

    /// <summary>
    /// 根据用户ID获取登录日志列表
    /// </summary>
    public async Task<List<RbacDtoBase>> GetByUserIdAsync(long userId)
    {
        var logs = await _loginLogRepository.GetByUserIdAsync(userId);
        return logs.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 根据用户名获取登录日志列表
    /// </summary>
    public async Task<List<RbacDtoBase>> GetByUsernameAsync(string username)
    {
        var logs = await _loginLogRepository.GetByUsernameAsync(username);
        return logs.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 根据IP地址获取登录日志列表
    /// </summary>
    public async Task<List<RbacDtoBase>> GetByIpAddressAsync(string ipAddress)
    {
        var logs = await _loginLogRepository.GetByIpAddressAsync(ipAddress);
        return logs.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 根据登录结果获取日志列表（成功/失败）
    /// </summary>
    public async Task<List<RbacDtoBase>> GetByLoginResultAsync(bool isSuccess)
    {
        var logs = await _loginLogRepository.GetByLoginResultAsync(isSuccess);
        return logs.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 获取失败登录日志
    /// </summary>
    public async Task<List<RbacDtoBase>> GetFailedLoginsAsync()
    {
        var logs = await _loginLogRepository.GetFailedLoginsAsync();
        return logs.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 根据时间范围获取日志列表
    /// </summary>
    public async Task<List<RbacDtoBase>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        var logs = await _loginLogRepository.GetByTimeRangeAsync(startTime, endTime);
        return logs.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 获取用户最近N次登录记录
    /// </summary>
    public async Task<List<RbacDtoBase>> GetRecentLoginsByUserIdAsync(long userId, int count = 10)
    {
        var logs = await _loginLogRepository.GetRecentLoginsByUserIdAsync(userId, count);
        return logs.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 获取分页列表
    /// </summary>
    public async Task<PageResponse<RbacDtoBase>> GetPagedAsync(PageQuery input)
    {
        var result = await _loginLogRepository.GetPagedAsync(input);
        var dtos = result.Items.Adapt<List<RbacDtoBase>>();
        return new PageResponse<RbacDtoBase>(dtos, result.PageData);
    }
}
