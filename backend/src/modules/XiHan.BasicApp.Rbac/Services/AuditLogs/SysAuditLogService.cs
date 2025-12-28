#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysAuditLogService
// Guid:g3c2d3e4-f5a6-7890-abcd-ef1234567904
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Extensions;
using XiHan.BasicApp.Rbac.Repositories.AuditLogs;
using XiHan.BasicApp.Rbac.Services.AuditLogs.Dtos;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.AuditLogs;

/// <summary>
/// 系统审核日志服务实现
/// </summary>
public class SysAuditLogService : CrudApplicationServiceBase<SysAuditLog, AuditLogDto, XiHanBasicAppIdType, CreateAuditLogDto, CreateAuditLogDto>, ISysAuditLogService
{
    private readonly ISysAuditLogRepository _auditLogRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysAuditLogService(ISysAuditLogRepository auditLogRepository) : base(auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    #region 业务特定方法

    /// <summary>
    /// 根据审核ID获取审核日志列表
    /// </summary>
    public async Task<List<AuditLogDto>> GetByAuditIdAsync(XiHanBasicAppIdType auditId)
    {
        var logs = await _auditLogRepository.GetByAuditIdAsync(auditId);
        return logs.ToDto();
    }

    /// <summary>
    /// 根据审核用户ID获取审核日志列表
    /// </summary>
    public async Task<List<AuditLogDto>> GetByAuditorIdAsync(XiHanBasicAppIdType auditorId)
    {
        var logs = await _auditLogRepository.GetByAuditorIdAsync(auditorId);
        return logs.ToDto();
    }

    /// <summary>
    /// 根据审核结果获取审核日志列表
    /// </summary>
    public async Task<List<AuditLogDto>> GetByResultAsync(AuditResult auditResult)
    {
        var logs = await _auditLogRepository.GetByResultAsync(auditResult);
        return logs.ToDto();
    }

    /// <summary>
    /// 根据时间范围获取审核日志列表
    /// </summary>
    public async Task<List<AuditLogDto>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        var logs = await _auditLogRepository.GetByTimeRangeAsync(startTime, endTime);
        return logs.ToDto();
    }

    #endregion 业务特定方法

    #region 映射方法实现

    /// <summary>
    /// 映射实体到DTO
    /// </summary>
    protected override Task<AuditLogDto> MapToEntityDtoAsync(SysAuditLog entity)
    {
        return Task.FromResult(entity.ToDto());
    }

    /// <summary>
    /// 映射 CreateAuditLogDto 到实体
    /// </summary>
    protected override Task<SysAuditLog> MapToEntityAsync(CreateAuditLogDto createDto)
    {
        var entity = new SysAuditLog
        {
            AuditId = createDto.AuditId,
            AuditorId = createDto.AuditorId,
            AuditLevel = createDto.AuditLevel,
            AuditResult = createDto.AuditResult,
            AuditOpinion = createDto.AuditOpinion,
            BeforeStatus = createDto.BeforeStatus,
            AfterStatus = createDto.AfterStatus,
            AuditIp = createDto.AuditIp,
            AuditLocation = createDto.AuditLocation,
            Browser = createDto.Browser,
            Os = createDto.Os,
            Attachments = createDto.Attachments,
            Remark = createDto.Remark
        };

        return Task.FromResult(entity);
    }

    protected override Task MapToEntityAsync(CreateAuditLogDto updateDto, SysAuditLog entity)
    {
        throw new NotImplementedException();
    }

    protected override Task<SysAuditLog> MapToEntityAsync(AuditLogDto dto)
    {
        throw new NotImplementedException();
    }

    protected override Task MapToEntityAsync(AuditLogDto dto, SysAuditLog entity)
    {
        throw new NotImplementedException();
    }

    #endregion 映射方法实现
}
