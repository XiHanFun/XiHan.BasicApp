#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ApiLogQueryService
// Guid:c5d6e7f8-a9b0-4c1d-2e3f-4a5b6c7d8e9f
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
/// API日志查询服务（处理API调用日志的读操作）
/// </summary>
public class ApiLogQueryService : ApplicationServiceBase
{
    private readonly IApiLogRepository _apiLogRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ApiLogQueryService(IApiLogRepository apiLogRepository)
    {
        _apiLogRepository = apiLogRepository;
    }

    /// <summary>
    /// 根据ID获取API日志
    /// </summary>
    public async Task<RbacDtoBase?> GetByIdAsync(long id)
    {
        var log = await _apiLogRepository.GetByIdAsync(id);
        return log?.Adapt<RbacDtoBase>();
    }

    /// <summary>
    /// 根据用户ID获取API日志列表
    /// </summary>
    public async Task<List<RbacDtoBase>> GetByUserIdAsync(long userId)
    {
        var logs = await _apiLogRepository.GetByUserIdAsync(userId);
        return logs.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 根据API路径获取日志列表
    /// </summary>
    public async Task<List<RbacDtoBase>> GetByApiPathAsync(string apiPath)
    {
        var logs = await _apiLogRepository.GetByApiPathAsync(apiPath);
        return logs.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 根据HTTP方法获取日志列表
    /// </summary>
    public async Task<List<RbacDtoBase>> GetByHttpMethodAsync(string httpMethod)
    {
        var logs = await _apiLogRepository.GetByHttpMethodAsync(httpMethod);
        return logs.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 根据HTTP状态码获取日志列表
    /// </summary>
    public async Task<List<RbacDtoBase>> GetByStatusCodeAsync(int statusCode)
    {
        var logs = await _apiLogRepository.GetByStatusCodeAsync(statusCode);
        return logs.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 根据时间范围获取日志列表
    /// </summary>
    public async Task<List<RbacDtoBase>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        var logs = await _apiLogRepository.GetByTimeRangeAsync(startTime, endTime);
        return logs.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 获取慢请求日志（响应时间超过阈值）
    /// </summary>
    public async Task<List<RbacDtoBase>> GetSlowRequestsAsync(int thresholdMs)
    {
        var logs = await _apiLogRepository.GetSlowRequestsAsync(thresholdMs);
        return logs.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 获取错误日志（HTTP状态码>=400）
    /// </summary>
    public async Task<List<RbacDtoBase>> GetErrorLogsAsync()
    {
        var logs = await _apiLogRepository.GetErrorLogsAsync();
        return logs.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 获取分页列表
    /// </summary>
    public async Task<PageResponse<RbacDtoBase>> GetPagedAsync(PageQuery input)
    {
        var result = await _apiLogRepository.GetPagedAsync(input);
        var dtos = result.Items.Adapt<List<RbacDtoBase>>();
        return new PageResponse<RbacDtoBase>(dtos, result.PageData);
    }
}
