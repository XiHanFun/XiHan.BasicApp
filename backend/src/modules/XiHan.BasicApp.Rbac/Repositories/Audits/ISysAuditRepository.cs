#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysAuditRepository
// Guid:a5b2c3d4-e5f6-7890-abcd-ef1234567894
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Audits;

/// <summary>
/// 系统审核仓储接口
/// </summary>
public interface ISysAuditRepository : IRepositoryBase<SysAudit, long>
{
    /// <summary>
    /// 根据提交用户ID获取审核列表
    /// </summary>
    /// <param name="submitterId">提交用户ID</param>
    /// <returns></returns>
    Task<List<SysAudit>> GetBySubmitterIdAsync(long submitterId);

    /// <summary>
    /// 根据审核用户ID获取审核列表
    /// </summary>
    /// <param name="auditorId">审核用户ID</param>
    /// <returns></returns>
    Task<List<SysAudit>> GetByAuditorIdAsync(long auditorId);

    /// <summary>
    /// 根据审核状态获取审核列表
    /// </summary>
    /// <param name="auditStatus">审核状态</param>
    /// <returns></returns>
    Task<List<SysAudit>> GetByStatusAsync(AuditStatus auditStatus);

    /// <summary>
    /// 根据业务类型和业务ID获取审核
    /// </summary>
    /// <param name="businessType">业务类型</param>
    /// <param name="businessId">业务ID</param>
    /// <returns></returns>
    Task<SysAudit?> GetByBusinessAsync(string businessType, long businessId);

    /// <summary>
    /// 根据租户ID获取审核列表
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns></returns>
    Task<List<SysAudit>> GetByTenantIdAsync(long tenantId);
}
