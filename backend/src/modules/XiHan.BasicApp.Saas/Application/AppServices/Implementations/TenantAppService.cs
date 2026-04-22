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
using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Application.UseCases.Commands;
using XiHan.BasicApp.Saas.Application.UseCases.Queries;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Uow;
using XiHan.Framework.Uow.Options;

namespace XiHan.BasicApp.Saas.Application.AppServices.Implementations;

/// <summary>
/// 租户应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统Saas服务")]
[Authorize]
[PermissionAuthorize("tenant:read")]
public class TenantAppService
    : CrudApplicationServiceBase<SysTenant, TenantDto, long, TenantCreateDto, TenantUpdateDto, BasicAppPRDto>,
        ITenantAppService
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantManager _tenantManager;
    private readonly ITenantQueryService _queryService;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="tenantRepository"></param>
    /// <param name="tenantManager"></param>
    /// <param name="queryService"></param>
    /// <param name="unitOfWorkManager"></param>
    public TenantAppService(
        ITenantRepository tenantRepository,
        ITenantManager tenantManager,
        ITenantQueryService queryService,
        IUnitOfWorkManager unitOfWorkManager)
        : base(tenantRepository)
    {
        _tenantRepository = tenantRepository;
        _tenantManager = tenantManager;
        _queryService = queryService;
        _unitOfWorkManager = unitOfWorkManager;
    }

    /// <summary>
    /// ID 查询（委托 QueryService，走缓存）
    /// </summary>
    [PermissionAuthorize("tenant:read")]
    public override async Task<TenantDto?> GetByIdAsync(long id)
    {
        return await _queryService.GetByIdAsync(id);
    }

    /// <summary>
    /// 根据租户编码获取租户
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    [PermissionAuthorize("tenant:read")]
    public async Task<TenantDto?> GetByCodeAsync(TenantByCodeQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await _queryService.GetByCodeAsync(query.TenantCode);
    }

    /// <summary>
    /// 创建租户
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [PermissionAuthorize("tenant:create")]
    public override async Task<TenantDto> CreateAsync(TenantCreateDto input)
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
            TenantStatus = TenantStatus.Normal
        };

        var created = await _tenantManager.CreateAsync(tenant);
        await uow.CompleteAsync();
        return created.Adapt<TenantDto>()!;
    }

    /// <summary>
    /// 更新租户
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [PermissionAuthorize("tenant:update")]
    public override async Task<TenantDto> UpdateAsync(TenantUpdateDto input)
    {
        input.ValidateAnnotations();

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

        if (input.TenantStatus == TenantStatus.Normal)
        {
            tenant.Enable();
        }
        else
        {
            tenant.Disable();
        }

        var updated = await _tenantRepository.UpdateAsync(tenant);
        await uow.CompleteAsync();
        return updated.Adapt<TenantDto>()!;
    }

    /// <summary>
    /// 修改租户状态
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    [PermissionAuthorize("tenant:update")]
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
