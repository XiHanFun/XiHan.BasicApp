#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysAuditRepository
// Guid:b5b2c3d4-e5f6-7890-abcd-ef1234567894
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Rbac.Repositories.Audits;

/// <summary>
/// 系统审核仓储实现
/// </summary>
public class SysAuditRepository : SqlSugarRepositoryBase<SysAudit, long>, ISysAuditRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public SysAuditRepository(ISqlSugarDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据提交用户ID获取审核列表
    /// </summary>
    public async Task<List<SysAudit>> GetBySubmitterIdAsync(long submitterId)
    {
        var result = await GetListAsync(audit => audit.SubmitterId == submitterId);
        return [.. result.OrderByDescending(audit => audit.SubmitTime)];
    }

    /// <summary>
    /// 根据审核用户ID获取审核列表
    /// </summary>
    public async Task<List<SysAudit>> GetByAuditorIdAsync(long auditorId)
    {
        var result = await GetListAsync(audit => audit.AuditorId == auditorId);
        return [.. result.OrderByDescending(audit => audit.SubmitTime)];
    }

    /// <summary>
    /// 根据审核状态获取审核列表
    /// </summary>
    public async Task<List<SysAudit>> GetByStatusAsync(AuditStatus auditStatus)
    {
        var result = await GetListAsync(audit => audit.AuditStatus == auditStatus);
        return [.. result.OrderByDescending(audit => audit.SubmitTime)];
    }

    /// <summary>
    /// 根据业务类型和业务ID获取审核
    /// </summary>
    public async Task<SysAudit?> GetByBusinessAsync(string businessType, long businessId)
    {
        return await GetFirstAsync(audit => audit.BusinessType == businessType && audit.BusinessId == businessId);
    }

    /// <summary>
    /// 根据租户ID获取审核列表
    /// </summary>
    public async Task<List<SysAudit>> GetByTenantIdAsync(long tenantId)
    {
        var result = await GetListAsync(audit => audit.TenantId == tenantId);
        return [.. result.OrderByDescending(audit => audit.SubmitTime)];
    }
}
