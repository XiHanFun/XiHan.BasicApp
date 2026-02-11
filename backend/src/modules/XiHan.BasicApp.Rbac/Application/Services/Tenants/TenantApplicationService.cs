#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantApplicationService
// Guid:f6a2ced9-8694-4e42-b3db-aa94cd996470
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/31 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Application.Services.Tenants.Dtos;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.BasicApp.Rbac.Domain.Services;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Application.Services.Tenants;

/// <summary>
/// 租户应用服务
/// </summary>
public class TenantApplicationService : CrudApplicationServiceBase<SysTenant, SysTenantDto, long, CreateSysTenantDto, UpdateSysTenantDto, SysTenantPageRequestDto>
{
    private readonly ISysTenantRepository _tenantRepository;
    private readonly ITenantManagementService _tenantManagementService;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TenantApplicationService(
        ISysTenantRepository tenantRepository,
        ITenantManagementService tenantManagementService,
        IUnitOfWorkManager unitOfWorkManager)
        : base(tenantRepository)
    {
        _tenantRepository = tenantRepository;
        _tenantManagementService = tenantManagementService;
        _unitOfWorkManager = unitOfWorkManager;
    }

    /// <summary>
    /// 创建租户
    /// </summary>
    public override async Task<SysTenantDto> CreateAsync(CreateSysTenantDto input)
    {
        //using var uow = _unitOfWorkManager.Begin();

        // 检查租户编码是否已存在
        var exists = await _tenantManagementService.IsTenantCodeDuplicateAsync(input.TenantCode);
        if (exists)
        {
            throw new InvalidOperationException($"租户编码 {input.TenantCode} 已存在");
        }

        // 检查域名是否重复
        if (!string.IsNullOrEmpty(input.Domain))
        {
            var domainExists = await _tenantManagementService.IsDomainDuplicateAsync(input.Domain);
            if (domainExists)
            {
                throw new InvalidOperationException($"域名 {input.Domain} 已存在");
            }
        }

        // 创建租户实体
        var tenant = input.Adapt<SysTenant>();
        tenant.Status = YesOrNo.Yes;
        tenant.TenantStatus = TenantStatus.Normal;
        tenant.ConfigStatus = TenantConfigStatus.Pending;
        tenant.CreatedTime = DateTimeOffset.Now;

        // 保存租户
        tenant = await _tenantRepository.SaveAsync(tenant);

        // 初始化租户数据
        await _tenantManagementService.InitializeTenantDataAsync(tenant);

        //await uow.CompleteAsync();

        return tenant.Adapt<SysTenantDto>();
    }

    /// <summary>
    /// 更新租户
    /// </summary>
    public override async Task<SysTenantDto> UpdateAsync(long id, UpdateSysTenantDto input)
    {
        //using var uow = _unitOfWorkManager.Begin();

        var tenant = await _tenantRepository.GetByIdAsync(id);
        if (tenant == null)
        {
            throw new KeyNotFoundException($"租户 {id} 不存在");
        }

        // 映射更新数据
        input.Adapt(tenant);
        tenant.ModifiedTime = DateTimeOffset.Now;

        // 保存租户
        tenant = await _tenantRepository.SaveAsync(tenant);

        //await uow.CompleteAsync();

        return tenant.Adapt<SysTenantDto>();
    }

    /// <summary>
    /// 启用租户
    /// </summary>
    public async Task<bool> EnableAsync(long tenantId)
    {
        //using var uow = _unitOfWorkManager.Begin();
        await _tenantRepository.EnableTenantAsync(tenantId);
        //await uow.CompleteAsync();
        return true;
    }

    /// <summary>
    /// 禁用租户
    /// </summary>
    public async Task<bool> DisableAsync(long tenantId)
    {
        //using var uow = _unitOfWorkManager.Begin();
        await _tenantRepository.DisableTenantAsync(tenantId);
        //await uow.CompleteAsync();
        return true;
    }

    /// <summary>
    /// 续期租户
    /// </summary>
    public async Task<bool> RenewAsync(RenewTenantDto input)
    {
        //using var uow = _unitOfWorkManager.Begin();
        await _tenantRepository.RenewTenantAsync(input.TenantId, input.Days);
        //await uow.CompleteAsync();
        return true;
    }

    /// <summary>
    /// 获取所有活跃租户
    /// </summary>
    public async Task<List<SysTenantDto>> GetActiveTenantsAsync()
    {
        var tenants = await _tenantRepository.GetActivTenantsAsync();
        return tenants.Adapt<List<SysTenantDto>>();
    }

    /// <summary>
    /// 获取即将过期的租户
    /// </summary>
    public async Task<List<SysTenantDto>> GetExpiringTenantsAsync(int days)
    {
        var tenants = await _tenantRepository.GetExpiringTenantsAsync(days);
        return tenants.Adapt<List<SysTenantDto>>();
    }

    /// <summary>
    /// 根据租户编码获取租户
    /// </summary>
    public async Task<SysTenantDto?> GetByTenantCodeAsync(string tenantCode)
    {
        var tenant = await _tenantRepository.GetByTenantCodeAsync(tenantCode);
        return tenant?.Adapt<SysTenantDto>();
    }

    /// <summary>
    /// 根据域名获取租户
    /// </summary>
    public async Task<SysTenantDto?> GetByDomainAsync(string domain)
    {
        var tenant = await _tenantRepository.GetByDomainAsync(domain);
        return tenant?.Adapt<SysTenantDto>();
    }

    /// <summary>
    /// 检查租户是否活跃
    /// </summary>
    public async Task<bool> IsTenantActiveAsync(long tenantId)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId);
        if (tenant == null)
        {
            return false;
        }

        return _tenantManagementService.IsTenantActive(tenant);
    }
}
