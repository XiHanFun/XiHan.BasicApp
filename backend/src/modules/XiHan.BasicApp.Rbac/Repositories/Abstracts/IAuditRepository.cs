#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IAuditRepository
// Guid:f8a9b0c1-d2e3-4f5a-4b5c-7d8e9f0a1b2c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// 审计策略仓储接口
/// </summary>
public interface IAuditRepository : IAggregateRootRepository<SysAudit, long>
{
    /// <summary>
    /// 根据审计编码查询审计策略
    /// </summary>
    /// <param name="auditCode">审计编码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审计策略实体</returns>
    Task<SysAudit?> GetByAuditCodeAsync(string auditCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查审计编码是否存在
    /// </summary>
    /// <param name="auditCode">审计编码</param>
    /// <param name="excludeAuditId">排除的审计ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    Task<bool> ExistsByAuditCodeAsync(string auditCode, long? excludeAuditId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取启用的审计策略列表
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审计策略列表</returns>
    Task<List<SysAudit>> GetActiveAuditsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据审计类型获取审计策略
    /// </summary>
    /// <param name="auditType">审计类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审计策略列表</returns>
    Task<List<SysAudit>> GetByAuditTypeAsync(string auditType, CancellationToken cancellationToken = default);
}
