#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysApiLogService
// Guid:f2c2d3e4-f5a6-7890-abcd-ef1234567899
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Services.ApiLogs.Dtos;
using XiHan.Framework.Application.Services.Abstracts;

namespace XiHan.BasicApp.Rbac.Services.ApiLogs;

/// <summary>
/// 系统API日志服务接口
/// </summary>
public interface ISysApiLogService : ICrudApplicationService<ApiLogDto, XiHanBasicAppIdType, CreateApiLogDto, CreateApiLogDto>
{
    /// <summary>
    /// 根据用户ID获取API日志列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    Task<List<ApiLogDto>> GetByUserIdAsync(XiHanBasicAppIdType userId);

    /// <summary>
    /// 根据API路径获取日志列表
    /// </summary>
    /// <param name="apiPath">API路径</param>
    /// <returns></returns>
    Task<List<ApiLogDto>> GetByApiPathAsync(string apiPath);

    /// <summary>
    /// 根据租户ID获取API日志列表
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns></returns>
    Task<List<ApiLogDto>> GetByTenantIdAsync(XiHanBasicAppIdType tenantId);

    /// <summary>
    /// 根据时间范围获取API日志列表
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <returns></returns>
    Task<List<ApiLogDto>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime);

    /// <summary>
    /// 根据状态码获取API日志列表
    /// </summary>
    /// <param name="statusCode">状态码</param>
    /// <returns></returns>
    Task<List<ApiLogDto>> GetByStatusCodeAsync(int statusCode);
}
