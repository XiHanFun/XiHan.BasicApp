#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:LogApplicationService
// Guid:5dca2f1f-9fca-4f98-bdff-c9c65c14cbf9
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/12 16:34:20
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Rbac.ApplicationServices.Logging;

/// <summary>
/// 日志查询应用服务
/// </summary>
public class LoggingApplicationService : ApplicationServiceBase
{
    private readonly ISysAccessLogRepository _accessLogRepository;
    private readonly ISysOperationLogRepository _operationLogRepository;
    private readonly ISysExceptionLogRepository _exceptionLogRepository;
    private readonly ISysAuditLogRepository _auditLogRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public LoggingApplicationService(
        ISysAccessLogRepository accessLogRepository,
        ISysOperationLogRepository operationLogRepository,
        ISysExceptionLogRepository exceptionLogRepository,
        ISysAuditLogRepository auditLogRepository)
    {
        _accessLogRepository = accessLogRepository;
        _operationLogRepository = operationLogRepository;
        _exceptionLogRepository = exceptionLogRepository;
        _auditLogRepository = auditLogRepository;
    }

    /// <summary>
    /// 分页查询访问日志
    /// </summary>
    public Task<PageResultDtoBase<SysAccessLog>> GetAccessLogsPagedAsync(PageRequestDtoBase input, CancellationToken cancellationToken = default)
    {
        return _accessLogRepository.GetPagedAsync(input, cancellationToken);
    }

    /// <summary>
    /// 分页查询操作日志
    /// </summary>
    public Task<PageResultDtoBase<SysOperationLog>> GetOperationLogsPagedAsync(PageRequestDtoBase input, CancellationToken cancellationToken = default)
    {
        return _operationLogRepository.GetPagedAsync(input, cancellationToken);
    }

    /// <summary>
    /// 分页查询异常日志
    /// </summary>
    public Task<PageResultDtoBase<SysExceptionLog>> GetExceptionLogsPagedAsync(PageRequestDtoBase input, CancellationToken cancellationToken = default)
    {
        return _exceptionLogRepository.GetPagedAsync(input, cancellationToken);
    }

    /// <summary>
    /// 分页查询审计日志
    /// </summary>
    public Task<PageResultDtoBase<SysAuditLog>> GetAuditLogsPagedAsync(PageRequestDtoBase input, CancellationToken cancellationToken = default)
    {
        return _auditLogRepository.GetPagedAsync(input, cancellationToken);
    }
}
