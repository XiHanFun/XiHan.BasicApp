// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.AccessPolicies;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Extensions;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Conditions;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Authorization.Conditions;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Enums;
using XiHan.Framework.Domain.Shared.Paging.Models;
using XiHan.Framework.MultiTenancy.Abstractions;
using ConditionOperator = XiHan.BasicApp.Saas.Domain.Entities.ConditionOperator;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 访问策略查询应用服务：只看当前层（平台上下文看平台策略，租户上下文看本租户的）
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "访问策略")]
public sealed class AccessPolicyQueryService
    : SaasApplicationService, IAccessPolicyQueryService
{
    private readonly IAccessPolicyRepository _policyRepository;
    private readonly IAccessPolicyConditionRepository _conditionRepository;
    private readonly IConditionAttributeRegistry _attributes;
    private readonly AccessPolicyTrialService _trialService;
    private readonly ILoginBanService _banService;
    private readonly ISqlSugarClientResolver _clientResolver;
    private readonly IFieldSecurityService _fieldSecurity;
    private readonly ICurrentTenant _currentTenant;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AccessPolicyQueryService(
        IAccessPolicyRepository policyRepository,
        IAccessPolicyConditionRepository conditionRepository,
        IConditionAttributeRegistry attributes,
        AccessPolicyTrialService trialService,
        ILoginBanService banService,
        ISqlSugarClientResolver clientResolver,
        IFieldSecurityService fieldSecurity,
        ICurrentTenant currentTenant,
        TimeProvider timeProvider)
    {
        _policyRepository = policyRepository;
        _conditionRepository = conditionRepository;
        _attributes = attributes;
        _trialService = trialService;
        _banService = banService;
        _clientResolver = clientResolver;
        _fieldSecurity = fieldSecurity;
        _currentTenant = currentTenant;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// 访问策略分页列表
    /// </summary>
    [PermissionAuthorize(SaasPermissionCodes.AccessPolicy.Read)]
    [HttpPost]
    public async Task<PageResultDtoBase<AccessPolicyListItemDto>> GetAccessPolicyPageAsync(AccessPolicyPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();
        var request = BuildPageRequest(input);
        await _fieldSecurity.GuardQueryAsync(request.Conditions, typeof(SysAccessPolicy), cancellationToken);
        if (request.Conditions.Sorts.Count == 0)
        {
            request.Conditions.AddSort((SysAccessPolicy policy) => policy.Priority, SortDirection.Ascending, 0);
            request.Conditions.AddSort((SysAccessPolicy policy) => policy.CreatedTime, SortDirection.Descending, 1);
        }

        var policies = await _policyRepository.GetPagedAsync(request, cancellationToken);
        if (policies.Items.Count == 0)
        {
            return new PageResultDtoBase<AccessPolicyListItemDto>([], policies.Page);
        }

        var ids = policies.Items.Select(policy => policy.BasicId).ToArray();
        var conditions = (await _conditionRepository.GetListAsync(condition => ids.Contains(condition.PolicyId), condition => condition.ConditionGroup, cancellationToken))
            .ToLookup(condition => condition.PolicyId);
        var now = _timeProvider.GetUtcNow();
        var hitCounts = await GetRecentHitCountsAsync(ids, now, cancellationToken);
        var items = policies.Items
            .Select(policy => AccessPolicyApplicationMapper.ToListItemDto(policy, [.. conditions[policy.BasicId]], hitCounts.GetValueOrDefault(policy.BasicId), now))
            .ToList();
        return new PageResultDtoBase<AccessPolicyListItemDto>(items, policies.Page);
    }

    /// <summary>
    /// 访问策略详情
    /// </summary>
    [PermissionAuthorize(SaasPermissionCodes.AccessPolicy.Read)]
    public async Task<AccessPolicyDetailDto?> GetAccessPolicyDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "访问策略主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();
        var policy = await _policyRepository.GetByIdAsync(id, cancellationToken);
        if (policy is null)
        {
            return null;
        }

        var conditions = await _conditionRepository.GetByPolicyIdAsync(id, cancellationToken);
        var now = _timeProvider.GetUtcNow();
        var hitCounts = await GetRecentHitCountsAsync([id], now, cancellationToken);
        return AccessPolicyApplicationMapper.ToDetailDto(policy, conditions, hitCounts.GetValueOrDefault(id), now);
    }

    /// <summary>
    /// 访问策略能用的条件属性
    /// </summary>
    [PermissionAuthorize(SaasPermissionCodes.AccessPolicy.Read)]
    public Task<List<AccessPolicyAttributeDto>> GetAccessPolicyAttributesAsync()
    {
        var items = _attributes.Attributes
            .Select(descriptor => new AccessPolicyAttributeDto
            {
                Name = descriptor.Name,
                DisplayName = descriptor.DisplayName,
                Kind = descriptor.Kind,
                IsMultiValued = descriptor.IsMultiValued,
                IsFamily = descriptor.IsFamily,
                IsLegacy = string.Equals(descriptor.Name, SaasConditionAttributes.EnvironmentLocation, StringComparison.OrdinalIgnoreCase),
                IsBeforeAuthentication = AccessPolicyPhases.IsAvailableBeforeAuthentication(descriptor.Name),
                Aliases = [.. descriptor.Aliases ?? []],
                Operators = [.. ConditionOperatorRules.AllowedFor(descriptor).Select(@operator => (ConditionOperator)(int)@operator)]
            })
            .ToList();
        return Task.FromResult(items);
    }

    /// <summary>
    /// 试运行一条策略
    /// </summary>
    [HttpPost]
    [PermissionAuthorize(SaasPermissionCodes.AccessPolicy.Trial)]
    public Task<AccessPolicyTrialResultDto> GetAccessPolicyTrialAsync(AccessPolicyTrialInputDto input, CancellationToken cancellationToken = default)
    {
        return _trialService.TrialAsync(input, cancellationToken);
    }

    /// <summary>
    /// 当前的自动封禁；租户上下文返回空
    /// </summary>
    [PermissionAuthorize(SaasPermissionCodes.AccessPolicy.Read)]
    public async Task<List<LoginBanDto>> GetLoginBansAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTenant.Id is > 0)
        {
            return [];
        }

        var now = _timeProvider.GetUtcNow();
        var bans = await _banService.ListAsync(cancellationToken);
        return [.. bans.Select(ban => new LoginBanDto
        {
            Ip = ban.Ip,
            Until = ban.Until,
            RemainingSeconds = Math.Max(0, (long)Math.Ceiling((ban.Until - now).TotalSeconds))
        })];
    }

    private static BasicAppPRDto BuildPageRequest(AccessPolicyPageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Conditions = new QueryConditions()
        };
        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword<SysAccessPolicy>(
                input.Keyword.Trim(),
                policy => policy.PolicyCode,
                policy => policy.PolicyName,
                policy => policy.Description);
        }

        if (input.Action.HasValue)
        {
            request.Conditions.AddFilter((SysAccessPolicy policy) => policy.Action, input.Action.Value);
        }

        if (input.Status.HasValue)
        {
            request.Conditions.AddFilter((SysAccessPolicy policy) => policy.Status, input.Status.Value);
        }

        if (input.Conditions?.Sorts is { Count: > 0 } sorts)
        {
            _ = request.Conditions.AddSorts(sorts);
        }

        if (input.Conditions?.Filters is { Count: > 0 } filters)
        {
            _ = request.Conditions.AddFilters(filters);
        }

        return request;
    }

    private async Task<Dictionary<long, long>> GetRecentHitCountsAsync(long[] policyIds, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var since = now.AddHours(-24);
        var rows = await _clientResolver.GetCurrentClient().Queryable<SysAccessPolicyHit>()
            .Where(hit => hit.HitTime >= since && hit.PolicyId != null && policyIds.Contains(hit.PolicyId.Value))
            .SplitTable()
            .Select(hit => new { hit.PolicyId, hit.HitCount })
            .ToListAsync(cancellationToken);
        return rows
            .GroupBy(row => row.PolicyId!.Value)
            .ToDictionary(group => group.Key, group => group.Sum(row => (long)row.HitCount));
    }
}
