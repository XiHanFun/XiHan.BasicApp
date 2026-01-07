#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AccessLogQueryService
// Guid:d6e7f8a9-b0c1-4d2e-3f4a-5b6c7d8e9f0a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Repositories.Abstracts.Logs;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Domain.Paging.Dtos;

namespace XiHan.BasicApp.Rbac.Services.Application.Queries;

/// <summary>
/// 访问日志查询服务（处理访问日志的读操作）
/// </summary>
public class AccessLogQueryService : ApplicationServiceBase
{
    private readonly IAccessLogRepository _accessLogRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AccessLogQueryService(IAccessLogRepository accessLogRepository)
    {
        _accessLogRepository = accessLogRepository;
    }

    /// <summary>
    /// 根据ID获取访问日志
    /// </summary>
    public async Task<RbacDtoBase?> GetByIdAsync(long id)
    {
        var log = await _accessLogRepository.GetByIdAsync(id);
        return log?.Adapt<RbacDtoBase>();
    }

    /// <summary>
    /// 根据用户ID获取访问日志列表
    /// </summary>
    public async Task<List<RbacDtoBase>> GetByUserIdAsync(long userId)
    {
        var logs = await _accessLogRepository.GetByUserIdAsync(userId);
        return logs.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 根据资源ID获取访问日志列表
    /// </summary>
    public async Task<List<RbacDtoBase>> GetByResourceIdAsync(long resourceId)
    {
        var logs = await _accessLogRepository.GetByResourceIdAsync(resourceId);
        return logs.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 根据IP地址获取访问日志列表
    /// </summary>
    public async Task<List<RbacDtoBase>> GetByIpAddressAsync(string ipAddress)
    {
        var logs = await _accessLogRepository.GetByIpAddressAsync(ipAddress);
        return logs.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 根据访问结果获取日志列表（允许/拒绝）
    /// </summary>
    public async Task<List<RbacDtoBase>> GetByAccessResultAsync(bool isAllowed)
    {
        var logs = await _accessLogRepository.GetByAccessResultAsync(isAllowed);
        return logs.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 获取拒绝访问的日志
    /// </summary>
    public async Task<List<RbacDtoBase>> GetDeniedAccessLogsAsync()
    {
        var logs = await _accessLogRepository.GetDeniedAccessLogsAsync();
        return logs.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 根据时间范围获取日志列表
    /// </summary>
    public async Task<List<RbacDtoBase>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        var logs = await _accessLogRepository.GetByTimeRangeAsync(startTime, endTime);
        return logs.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 获取分页列表
    /// </summary>
    public async Task<PageResponse<RbacDtoBase>> GetPagedAsync(PageQuery input)
    {
        var result = await _accessLogRepository.GetPagedAsync(input);
        var dtos = result.Items.Adapt<List<RbacDtoBase>>();
        return new PageResponse<RbacDtoBase>(dtos, result.PageData);
    }
}
