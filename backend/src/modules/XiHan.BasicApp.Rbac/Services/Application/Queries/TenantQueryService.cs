#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantQueryService
// Guid:e2f3a4b5-c6d7-4e5f-8a9b-1c2d3e4f5a6b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Domain.Paging.Dtos;

namespace XiHan.BasicApp.Rbac.Services.Application.Queries;

/// <summary>
/// 租户查询服务（处理租户的读操作 - CQRS）
/// </summary>
public class TenantQueryService : ApplicationServiceBase
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TenantQueryService(
        ITenantRepository tenantRepository,
        IUserRepository userRepository)
    {
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
    }

    /// <summary>
    /// 根据ID获取租户
    /// </summary>
    /// <param name="id">租户ID</param>
    /// <returns>租户DTO</returns>
    public async Task<RbacDtoBase?> GetByIdAsync(long id)
    {
        var tenant = await _tenantRepository.GetByIdAsync(id);
        return tenant?.Adapt<RbacDtoBase>();
    }

    /// <summary>
    /// 根据租户编码获取租户
    /// </summary>
    /// <param name="tenantCode">租户编码</param>
    /// <returns>租户DTO</returns>
    public async Task<RbacDtoBase?> GetByTenantCodeAsync(string tenantCode)
    {
        var tenant = await _tenantRepository.GetByTenantCodeAsync(tenantCode);
        return tenant?.Adapt<RbacDtoBase>();
    }

    /// <summary>
    /// 根据域名获取租户
    /// </summary>
    /// <param name="domain">域名</param>
    /// <returns>租户DTO</returns>
    public async Task<RbacDtoBase?> GetByDomainAsync(string domain)
    {
        var tenant = await _tenantRepository.GetByDomainAsync(domain);
        return tenant?.Adapt<RbacDtoBase>();
    }

    /// <summary>
    /// 获取启用的租户列表
    /// </summary>
    /// <returns>租户DTO列表</returns>
    public async Task<List<RbacDtoBase>> GetActiveTenantsAsync()
    {
        var tenants = await _tenantRepository.GetActiveTenantsAsync();
        return tenants.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 获取租户的用户数量
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns>用户数量</returns>
    public async Task<int> GetUserCountAsync(long tenantId)
    {
        return await _tenantRepository.GetUserCountAsync(tenantId);
    }

    /// <summary>
    /// 获取租户的所有用户
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns>用户DTO列表</returns>
    public async Task<List<RbacDtoBase>> GetTenantUsersAsync(long tenantId)
    {
        var users = await _userRepository.GetByTenantIdAsync(tenantId);
        return users.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 获取分页列表
    /// </summary>
    /// <param name="input">分页查询参数</param>
    /// <returns>分页响应</returns>
    public async Task<PageResponse<RbacDtoBase>> GetPagedAsync(PageQuery input)
    {
        var result = await _tenantRepository.GetPagedAsync(input);
        var dtos = result.Items.Adapt<List<RbacDtoBase>>();
        return new PageResponse<RbacDtoBase>(dtos, result.PageData);
    }
}
