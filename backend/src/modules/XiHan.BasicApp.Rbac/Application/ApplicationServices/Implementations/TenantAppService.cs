#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantAppService
// Guid:bcbdbc76-392e-4054-9892-98146d887a3e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 06:29:49
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Application.Commands;
using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.BasicApp.Rbac.Application.Queries;
using XiHan.BasicApp.Rbac.Domain.DomainServices;
using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Uow;
using XiHan.Framework.Uow.Options;

namespace XiHan.BasicApp.Rbac.Application.ApplicationServices.Implementations;

/// <summary>
/// 租户应用服务
/// </summary>
public class TenantAppService : ApplicationServiceBase, ITenantAppService
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantManager _tenantManager;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="tenantRepository"></param>
    /// <param name="tenantManager"></param>
    /// <param name="unitOfWorkManager"></param>
    public TenantAppService(
        ITenantRepository tenantRepository,
        ITenantManager tenantManager,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _tenantRepository = tenantRepository;
        _tenantManager = tenantManager;
        _unitOfWorkManager = unitOfWorkManager;
    }

    /// <summary>
    /// 根据租户ID获取租户
    /// </summary>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    public async Task<TenantDto?> GetByIdAsync(long tenantId)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId);
        return tenant?.Adapt<TenantDto>();
    }

    /// <summary>
    /// 根据租户编码获取租户
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    public async Task<TenantDto?> GetByCodeAsync(TenantByCodeQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var tenant = await _tenantRepository.GetByTenantCodeAsync(query.TenantCode);
        return tenant?.Adapt<TenantDto>();
    }

    /// <summary>
    /// 创建租户
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public async Task<TenantDto> CreateAsync(TenantCreateDto input)
    {
        input.ValidateAnnotations();
        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);

        var tenant = new SysTenant
        {
            TenantCode = input.TenantCode.Trim(),
            TenantName = input.TenantName.Trim(),
            TenantShortName = input.TenantShortName,
            ContactPerson = input.ContactPerson,
            ContactPhone = input.ContactPhone,
            ContactEmail = input.ContactEmail,
            IsolationMode = input.IsolationMode,
            TenantStatus = TenantStatus.Normal,
            Status = YesOrNo.Yes
        };

        var created = await _tenantManager.CreateAsync(tenant);
        await uow.CompleteAsync();
        return created.Adapt<TenantDto>();
    }

    /// <summary>
    /// 更新租户
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public async Task<TenantDto> UpdateAsync(TenantUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.BasicId <= 0)
        {
            throw new ArgumentException("租户 ID 无效", nameof(input.BasicId));
        }

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        var tenant = await _tenantRepository.GetByIdAsync(input.BasicId)
                     ?? throw new KeyNotFoundException($"未找到租户: {input.BasicId}");

        tenant.TenantName = input.TenantName.Trim();
        tenant.TenantShortName = input.TenantShortName;
        tenant.ContactPerson = input.ContactPerson;
        tenant.ContactPhone = input.ContactPhone;
        tenant.ContactEmail = input.ContactEmail;
        tenant.ExpireTime = input.ExpireTime;
        tenant.Remark = input.Remark;

        if (input.Status == YesOrNo.Yes)
        {
            tenant.Enable();
        }
        else
        {
            tenant.Disable();
        }

        var updated = await _tenantRepository.UpdateAsync(tenant);
        await uow.CompleteAsync();
        return updated.Adapt<TenantDto>();
    }

    /// <summary>
    /// 修改租户状态
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    public async Task ChangeStatusAsync(ChangeTenantStatusCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.TenantId <= 0)
        {
            throw new ArgumentException("租户 ID 无效", nameof(command.TenantId));
        }

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        var tenant = await _tenantRepository.GetByIdAsync(command.TenantId)
                     ?? throw new KeyNotFoundException($"未找到租户: {command.TenantId}");

        await _tenantManager.ChangeStatusAsync(tenant, command.TenantStatus);
        await uow.CompleteAsync();
    }
}
