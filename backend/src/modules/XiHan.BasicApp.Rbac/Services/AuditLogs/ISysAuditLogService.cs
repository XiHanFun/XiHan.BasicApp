#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysAuditLogService
// Guid:f3c2d3e4-f5a6-7890-abcd-ef1234567900
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Services.AuditLogs.Dtos;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.AuditLogs;

/// <summary>
/// 系统审核日志服务接口
/// </summary>
public interface ISysAuditLogService : ICrudApplicationService<AuditLogDto, XiHanBasicAppIdType, CreateAuditLogDto, CreateAuditLogDto>
{
    /// <summary>
    /// 根据审核ID获取审核日志列表
    /// </summary>
    /// <param name="auditId">审核ID</param>
    /// <returns></returns>
    Task<List<AuditLogDto>> GetByAuditIdAsync(XiHanBasicAppIdType auditId);

    /// <summary>
    /// 根据审核用户ID获取审核日志列表
    /// </summary>
    /// <param name="auditorId">审核用户ID</param>
    /// <returns></returns>
    Task<List<AuditLogDto>> GetByAuditorIdAsync(XiHanBasicAppIdType auditorId);

    /// <summary>
    /// 根据审核结果获取审核日志列表
    /// </summary>
    /// <param name="auditResult">审核结果</param>
    /// <returns></returns>
    Task<List<AuditLogDto>> GetByResultAsync(AuditResult auditResult);

    /// <summary>
    /// 根据时间范围获取审核日志列表
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <returns></returns>
    Task<List<AuditLogDto>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime);
}
