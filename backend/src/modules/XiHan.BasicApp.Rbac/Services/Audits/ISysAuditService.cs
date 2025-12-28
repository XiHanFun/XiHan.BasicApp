#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysAuditService
// Guid:f1g2h3i4-j5k6-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 16:20:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Services.Audits.Dtos;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.Audits;

/// <summary>
/// 系统审核服务接口
/// </summary>
public interface ISysAuditService : ICrudApplicationService<AuditDto, XiHanBasicAppIdType, CreateAuditDto, UpdateAuditDto>
{
    /// <summary>
    /// 根据提交用户ID获取审核列表
    /// </summary>
    /// <param name="submitterId">提交用户ID</param>
    /// <returns></returns>
    Task<List<AuditDto>> GetBySubmitterIdAsync(XiHanBasicAppIdType submitterId);

    /// <summary>
    /// 根据审核用户ID获取审核列表
    /// </summary>
    /// <param name="auditorId">审核用户ID</param>
    /// <returns></returns>
    Task<List<AuditDto>> GetByAuditorIdAsync(XiHanBasicAppIdType auditorId);

    /// <summary>
    /// 根据审核状态获取审核列表
    /// </summary>
    /// <param name="auditStatus">审核状态</param>
    /// <returns></returns>
    Task<List<AuditDto>> GetByStatusAsync(AuditStatus auditStatus);

    /// <summary>
    /// 根据业务类型和业务ID获取审核
    /// </summary>
    /// <param name="businessType">业务类型</param>
    /// <param name="businessId">业务ID</param>
    /// <returns></returns>
    Task<AuditDto?> GetByBusinessAsync(string businessType, XiHanBasicAppIdType businessId);

    /// <summary>
    /// 根据租户ID获取审核列表
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns></returns>
    Task<List<AuditDto>> GetByTenantIdAsync(XiHanBasicAppIdType tenantId);

    /// <summary>
    /// 处理审核
    /// </summary>
    /// <param name="input">审核处理DTO</param>
    /// <returns></returns>
    Task<bool> ProcessAuditAsync(ProcessAuditDto input);
}

