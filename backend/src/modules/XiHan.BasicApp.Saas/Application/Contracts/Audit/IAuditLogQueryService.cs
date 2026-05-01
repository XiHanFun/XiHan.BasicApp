#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IAuditLogQueryService
// Guid:dc1272c4-445f-4fbf-9f6e-4f5a56ad7d8f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 审计日志查询应用服务接口
/// </summary>
public interface IAuditLogQueryService : IApplicationService
{
    /// <summary>
    /// 获取审计日志分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审计日志分页列表</returns>
    Task<PageResultDtoBase<AuditLogListItemDto>> GetAuditLogPageAsync(AuditLogPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取审计日志详情
    /// </summary>
    /// <param name="id">审计日志主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审计日志详情</returns>
    Task<AuditLogDetailDto?> GetAuditLogDetailAsync(long id, CancellationToken cancellationToken = default);
}
