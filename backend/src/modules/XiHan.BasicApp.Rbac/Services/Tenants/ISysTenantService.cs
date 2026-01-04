#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysTenantService
// Guid:3b2b3c4d-5e6f-7890-abcd-ef12345678a8
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 5:30:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Services.Tenants.Dtos;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Application.Services.Abstracts;
using XiHan.Framework.Domain.Paging.Dtos;

namespace XiHan.BasicApp.Rbac.Services.Tenants;

/// <summary>
/// 系统租户服务接口
/// </summary>
public interface ISysTenantService : ICrudApplicationService<TenantDto, XiHanBasicAppIdType, CreateTenantDto, UpdateTenantDto>
{
    /// <summary>
    /// 获取租户详情
    /// </summary>
    /// <param name="id">租户ID</param>
    /// <returns></returns>
    Task<TenantDetailDto?> GetDetailAsync(XiHanBasicAppIdType id);

    /// <summary>
    /// 根据租户编码获取租户
    /// </summary>
    /// <param name="tenantCode">租户编码</param>
    /// <returns></returns>
    Task<TenantDto?> GetByTenantCodeAsync(string tenantCode);

    /// <summary>
    /// 根据域名获取租户
    /// </summary>
    /// <param name="domain">域名</param>
    /// <returns></returns>
    Task<TenantDto?> GetByDomainAsync(string domain);

    /// <summary>
    /// 配置租户数据库
    /// </summary>
    /// <param name="input">配置租户数据库DTO</param>
    /// <returns></returns>
    Task<bool> ConfigureDatabaseAsync(ConfigureTenantDatabaseDto input);
}
