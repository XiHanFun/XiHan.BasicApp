#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysApiLogService
// Guid:g2c2d3e4-f5a6-7890-abcd-ef1234567903
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Extensions;
using XiHan.BasicApp.Rbac.Repositories.ApiLogs;
using XiHan.BasicApp.Rbac.Services.AccessLogs.Dtos;
using XiHan.BasicApp.Rbac.Services.ApiLogs.Dtos;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.ApiLogs;

/// <summary>
/// 系统API日志服务实现
/// </summary>
public class SysApiLogService : CrudApplicationServiceBase<SysApiLog, ApiLogDto, XiHanBasicAppIdType, CreateApiLogDto, CreateApiLogDto>, ISysApiLogService
{
    private readonly ISysApiLogRepository _apiLogRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysApiLogService(ISysApiLogRepository apiLogRepository) : base(apiLogRepository)
    {
        _apiLogRepository = apiLogRepository;
    }

    #region 业务特定方法

    /// <summary>
    /// 根据用户ID获取API日志列表
    /// </summary>
    public async Task<List<ApiLogDto>> GetByUserIdAsync(XiHanBasicAppIdType userId)
    {
        var logs = await _apiLogRepository.GetByUserIdAsync(userId);
        return logs.Adapt<List<ApiLogDto>>();
    }

    /// <summary>
    /// 根据API路径获取日志列表
    /// </summary>
    public async Task<List<ApiLogDto>> GetByApiPathAsync(string apiPath)
    {
        var logs = await _apiLogRepository.GetByApiPathAsync(apiPath);
        return logs.Adapt<List<ApiLogDto>>();
    }

    /// <summary>
    /// 根据租户ID获取API日志列表
    /// </summary>
    public async Task<List<ApiLogDto>> GetByTenantIdAsync(XiHanBasicAppIdType tenantId)
    {
        var logs = await _apiLogRepository.GetByTenantIdAsync(tenantId);
        return logs.Adapt<List<ApiLogDto>>();
    }

    /// <summary>
    /// 根据时间范围获取API日志列表
    /// </summary>
    public async Task<List<ApiLogDto>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        var logs = await _apiLogRepository.GetByTimeRangeAsync(startTime, endTime);
        return logs.Adapt<List<ApiLogDto>>();
    }

    /// <summary>
    /// 根据状态码获取API日志列表
    /// </summary>
    public async Task<List<ApiLogDto>> GetByStatusCodeAsync(int statusCode)
    {
        var logs = await _apiLogRepository.GetByStatusCodeAsync(statusCode);
        return logs.Adapt<List<ApiLogDto>>();
    }

    #endregion 业务特定方法
}
