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

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Extensions;
using XiHan.BasicApp.Rbac.Repositories.Audits;
using XiHan.BasicApp.Rbac.Services.Audits.Dtos;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.Audits;

/// <summary>
/// 系统审核服务实现
/// </summary>
public class SysAuditService : CrudApplicationServiceBase<SysAudit, AuditDto, XiHanBasicAppIdType, CreateAuditDto, UpdateAuditDto>, ISysAuditService
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
    public async Task<List<AuditDto>> GetBySubmitterIdAsync(XiHanBasicAppIdType submitterId)
    {
        var audits = await _auditRepository.GetBySubmitterIdAsync(submitterId);
        return audits.ToDto();
    }

    /// <summary>
    /// 根据审核用户ID获取审核列表
    /// </summary>
    public async Task<List<AuditDto>> GetByAuditorIdAsync(XiHanBasicAppIdType auditorId)
    {
        var audits = await _auditRepository.GetByAuditorIdAsync(auditorId);
        return audits.ToDto();
    }

    /// <summary>
    /// 根据审核状态获取审核列表
    /// </summary>
    public async Task<List<AuditDto>> GetByStatusAsync(AuditStatus auditStatus)
    {
        var audits = await _auditRepository.GetByStatusAsync(auditStatus);
        return audits.ToDto();
    }

    /// <summary>
    /// 根据业务类型和业务ID获取审核
    /// </summary>
    public async Task<AuditDto?> GetByBusinessAsync(string businessType, XiHanBasicAppIdType businessId)
    {
        var audit = await _auditRepository.GetByBusinessAsync(businessType, businessId);
        return audit?.ToDto();
    }

    /// <summary>
    /// 根据租户ID获取审核列表
    /// </summary>
    public async Task<List<AuditDto>> GetByTenantIdAsync(XiHanBasicAppIdType tenantId)
    {
        var audits = await _auditRepository.GetByTenantIdAsync(tenantId);
        return audits.ToDto();
    }

    /// <summary>
    /// 处理审核
    /// </summary>
    public async Task<bool> ProcessAuditAsync(ProcessAuditDto input)
    {
        var audit = await _auditRepository.GetByIdAsync(input.AuditId);
        if (audit == null)
        {
            throw new InvalidOperationException("审核记录不存在");
        }

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

    #region 映射方法实现

    /// <summary>
    /// 映射实体到DTO
    /// </summary>
    protected override Task<AuditDto> MapToEntityDtoAsync(SysAudit entity)
    {
        return Task.FromResult(entity.ToDto());
    }

    /// <summary>
    /// 映射 AuditDto 到实体（基类方法，不推荐直接使用）
    /// </summary>
    protected override Task<SysAudit> MapToEntityAsync(AuditDto dto)
    {
        var entity = new SysAudit
        {
            TenantId = dto.TenantId,
            Title = dto.Title,
            Content = dto.Content,
            BusinessType = dto.BusinessType,
            BusinessId = dto.BusinessId,
            BusinessData = dto.BusinessData,
            SubmitterId = dto.SubmitterId,
            AuditorId = dto.AuditorId,
            AuditStatus = dto.AuditStatus,
            AuditResult = dto.AuditResult,
            AuditOpinion = dto.AuditOpinion,
            SubmitTime = dto.SubmitTime,
            AuditTime = dto.AuditTime,
            Priority = dto.Priority,
            IsMultiLevel = dto.IsMultiLevel,
            CurrentLevel = dto.CurrentLevel,
            TotalLevel = dto.TotalLevel,
            Deadline = dto.Deadline,
            Attachments = dto.Attachments,
            Remark = dto.Remark
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射 AuditDto 到现有实体（基类方法，不推荐直接使用）
    /// </summary>
    protected override Task MapToEntityAsync(AuditDto dto, SysAudit entity)
    {
        entity.Title = dto.Title;
        entity.Content = dto.Content;
        entity.BusinessData = dto.BusinessData;
        entity.Priority = dto.Priority;
        entity.Deadline = dto.Deadline;
        entity.Attachments = dto.Attachments;
        entity.Remark = dto.Remark;

        return Task.CompletedTask;
    }

    /// <summary>
    /// 映射创建DTO到实体
    /// </summary>
    protected override Task<SysAudit> MapToEntityAsync(CreateAuditDto createDto)
    {
        var entity = new SysAudit
        {
            TenantId = createDto.TenantId,
            Title = createDto.Title,
            Content = createDto.Content,
            BusinessType = createDto.BusinessType,
            BusinessId = createDto.BusinessId,
            BusinessData = createDto.BusinessData,
            SubmitterId = createDto.SubmitterId,
            SubmitTime = DateTimeOffset.Now,
            Priority = createDto.Priority,
            IsMultiLevel = createDto.IsMultiLevel,
            CurrentLevel = 1,
            TotalLevel = createDto.TotalLevel,
            Deadline = createDto.Deadline,
            Attachments = createDto.Attachments,
            Remark = createDto.Remark
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射更新DTO到现有实体
    /// </summary>
    protected override Task MapToEntityAsync(UpdateAuditDto updateDto, SysAudit entity)
    {
        if (updateDto.Title != null) entity.Title = updateDto.Title;
        if (updateDto.Content != null) entity.Content = updateDto.Content;
        if (updateDto.BusinessData != null) entity.BusinessData = updateDto.BusinessData;
        if (updateDto.Priority.HasValue) entity.Priority = updateDto.Priority.Value;
        if (updateDto.Deadline.HasValue) entity.Deadline = updateDto.Deadline;
        if (updateDto.Attachments != null) entity.Attachments = updateDto.Attachments;
        if (updateDto.Remark != null) entity.Remark = updateDto.Remark;

        return Task.CompletedTask;
    }

    #endregion 映射方法实现
}

