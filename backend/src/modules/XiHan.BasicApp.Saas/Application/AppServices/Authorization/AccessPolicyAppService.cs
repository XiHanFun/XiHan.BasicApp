// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.AccessPolicies;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 访问策略命令应用服务
/// </summary>
/// <remarks>
/// 写入后在事务提交时换新的策略版本号。保存后是启用中的拒绝策略时，先拿当前请求试一次，会拦下当前请求就整笔回滚。
/// </remarks>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "访问策略")]
public sealed class AccessPolicyAppService
    : SaasApplicationService, IAccessPolicyAppService
{
    private readonly IAccessPolicyDomainService _domainService;
    private readonly IAccessPolicyQueryService _queryService;
    private readonly AccessPolicyTrialService _trialService;
    private readonly IAccessPolicySnapshotProvider _snapshots;
    private readonly ILoginBanService _banService;
    private readonly ICurrentTenant _currentTenant;
    private readonly IFieldSecurityService _fieldSecurity;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AccessPolicyAppService(
        IAccessPolicyDomainService domainService,
        IAccessPolicyQueryService queryService,
        AccessPolicyTrialService trialService,
        IAccessPolicySnapshotProvider snapshots,
        ILoginBanService banService,
        ICurrentTenant currentTenant,
        IFieldSecurityService fieldSecurity)
    {
        _domainService = domainService;
        _queryService = queryService;
        _trialService = trialService;
        _snapshots = snapshots;
        _banService = banService;
        _currentTenant = currentTenant;
        _fieldSecurity = fieldSecurity;
    }

    /// <summary>
    /// 创建访问策略（一律只记录）
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.AccessPolicy.Create)]
    public async Task<AccessPolicyDetailDto> CreateAccessPolicyAsync(AccessPolicyCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();
        await _fieldSecurity.EnsureCreatableAsync(typeof(SysAccessPolicy), input, cancellationToken);
        var result = await _domainService.CreateAccessPolicyAsync(AccessPolicyApplicationMapper.ToCreateCommand(input), cancellationToken);
        await _snapshots.InvalidateAsync(cancellationToken);
        return await GetDetailOrThrowAsync(result.PolicyId, cancellationToken);
    }

    /// <summary>
    /// 更新访问策略的定义与条件
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.AccessPolicy.Update)]
    public async Task<AccessPolicyDetailDto> UpdateAccessPolicyAsync(AccessPolicyUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();
        await _fieldSecurity.EnsureUpdatableAsync(typeof(SysAccessPolicy), input.BasicId, input, cancellationToken);
        var result = await _domainService.UpdateAccessPolicyAsync(AccessPolicyApplicationMapper.ToUpdateCommand(input), cancellationToken);
        return await OnChangedAsync(result.PolicyId, cancellationToken);
    }

    /// <summary>
    /// 修改访问策略的动作（含启用拦截）
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.AccessPolicy.Update)]
    public async Task<AccessPolicyDetailDto> UpdateAccessPolicyActionAsync(AccessPolicyActionUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();
        await _fieldSecurity.EnsureUpdatableAsync(typeof(SysAccessPolicy), input.BasicId, input, cancellationToken);
        var result = await _domainService.UpdateAccessPolicyActionAsync(new AccessPolicyActionCommand(input.BasicId, input.Action), cancellationToken);
        return await OnChangedAsync(result.PolicyId, cancellationToken);
    }

    /// <summary>
    /// 启用或停用访问策略
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.AccessPolicy.Status)]
    public async Task<AccessPolicyDetailDto> UpdateAccessPolicyStatusAsync(AccessPolicyStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();
        await _fieldSecurity.EnsureUpdatableAsync(typeof(SysAccessPolicy), input.BasicId, input, cancellationToken);
        var result = await _domainService.UpdateAccessPolicyStatusAsync(new AccessPolicyStatusCommand(input.BasicId, input.Status), cancellationToken);
        return await OnChangedAsync(result.PolicyId, cancellationToken);
    }

    /// <summary>
    /// 删除访问策略
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.AccessPolicy.Delete)]
    public async Task DeleteAccessPolicyAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _domainService.DeleteAccessPolicyAsync(id, cancellationToken);
        await _snapshots.InvalidateAsync(cancellationToken);
    }

    /// <summary>
    /// 手动解除自动封禁（只在平台）
    /// </summary>
    [PermissionAuthorize(SaasPermissionCodes.AccessPolicy.Ban)]
    public async Task<bool> UnbanLoginAsync(LoginUnbanDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();
        if (_currentTenant.Id is > 0)
        {
            throw new InvalidOperationException("自动封禁只能在平台解除。");
        }

        if (string.IsNullOrWhiteSpace(input.Ip))
        {
            throw new InvalidOperationException("IP 不能为空。");
        }

        return await _banService.UnbanAsync(input.Ip.Trim(), cancellationToken);
    }

    private async Task<AccessPolicyDetailDto> OnChangedAsync(long policyId, CancellationToken cancellationToken)
    {
        await _trialService.EnsureNotBlockingCurrentRequestAsync(policyId, cancellationToken);
        await _snapshots.InvalidateAsync(cancellationToken);
        return await GetDetailOrThrowAsync(policyId, cancellationToken);
    }

    private async Task<AccessPolicyDetailDto> GetDetailOrThrowAsync(long policyId, CancellationToken cancellationToken)
    {
        return await _queryService.GetAccessPolicyDetailAsync(policyId, cancellationToken)
            ?? throw new InvalidOperationException("访问策略不存在。");
    }
}
