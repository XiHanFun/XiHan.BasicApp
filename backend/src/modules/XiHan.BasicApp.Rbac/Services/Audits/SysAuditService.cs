#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysAuditService
// Guid:g1h2i3j4-k5l6-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 16:25:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Repositories.Audits;
using XiHan.BasicApp.Rbac.Services.Audits.Dtos;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.Audits;

/// <summary>
/// 系统审核服务实现
/// </summary>
public class SysAuditService : CrudApplicationServiceBase<SysAudit, AuditDto, long, CreateAuditDto, UpdateAuditDto>, ISysAuditService
{
    private readonly ISysAuditRepository _auditRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysAuditService(ISysAuditRepository auditRepository) : base(auditRepository)
    {
        _auditRepository = auditRepository;
    }

    #region 业务特定方法

    /// <summary>
    /// 根据提交用户ID获取审核列表
    /// </summary>
    public async Task<List<AuditDto>> GetBySubmitterIdAsync(long submitterId)
    {
        var audits = await _auditRepository.GetBySubmitterIdAsync(submitterId);
        return audits.Adapt<List<AuditDto>>();
    }

    /// <summary>
    /// 根据审核用户ID获取审核列表
    /// </summary>
    public async Task<List<AuditDto>> GetByAuditorIdAsync(long auditorId)
    {
        var audits = await _auditRepository.GetByAuditorIdAsync(auditorId);
        return audits.Adapt<List<AuditDto>>();
    }

    /// <summary>
    /// 根据审核状态获取审核列表
    /// </summary>
    public async Task<List<AuditDto>> GetByStatusAsync(AuditStatus auditStatus)
    {
        var audits = await _auditRepository.GetByStatusAsync(auditStatus);
        return audits.Adapt<List<AuditDto>>();
    }

    /// <summary>
    /// 根据业务类型和业务ID获取审核
    /// </summary>
    public async Task<AuditDto?> GetByBusinessAsync(string businessType, long businessId)
    {
        var audit = await _auditRepository.GetByBusinessAsync(businessType, businessId);
        return audit.Adapt<AuditDto>();
    }

    /// <summary>
    /// 根据租户ID获取审核列表
    /// </summary>
    public async Task<List<AuditDto>> GetByTenantIdAsync(long tenantId)
    {
        var audits = await _auditRepository.GetByTenantIdAsync(tenantId);
        return audits.Adapt<List<AuditDto>>();
    }

    /// <summary>
    /// 处理审核
    /// </summary>
    public async Task<bool> ProcessAuditAsync(ProcessAuditDto input)
    {
        var audit = await _auditRepository.GetByIdAsync(input.AuditId) ?? throw new InvalidOperationException("审核记录不存在");
        if (audit.AuditStatus != AuditStatus.Pending && audit.AuditStatus != AuditStatus.InProgress)
        {
            throw new InvalidOperationException("当前审核状态不允许处理");
        }

        audit.AuditorId = input.AuditorId;
        audit.AuditResult = input.AuditResult;
        audit.AuditOpinion = input.AuditOpinion;
        audit.AuditTime = DateTimeOffset.Now;

        // 根据审核结果更新审核状态
        audit.AuditStatus = input.AuditResult switch
        {
            AuditResult.Pass => audit.IsMultiLevel && audit.CurrentLevel < audit.TotalLevel
                ? AuditStatus.InProgress
                : AuditStatus.Approved,
            AuditResult.Reject => AuditStatus.Rejected,
            AuditResult.Return => AuditStatus.Pending,
            _ => audit.AuditStatus
        };

        // 如果是多级审核且通过，增加审核级别
        if (input.AuditResult == AuditResult.Pass && audit.IsMultiLevel && audit.CurrentLevel < audit.TotalLevel)
        {
            audit.CurrentLevel++;
        }

        await _auditRepository.UpdateAsync(audit);
        return true;
    }

    #endregion 业务特定方法
}
