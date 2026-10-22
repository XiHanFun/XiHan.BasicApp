// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Extensions;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Models;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 条件命中记录查询应用服务：只看当前层（平台看平台策略的命中，租户看本租户策略与权限条件的命中）
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "条件命中记录")]
public sealed class AccessPolicyHitQueryService
    : SaasApplicationService, IAccessPolicyHitQueryService
{
    private readonly ISqlSugarClientResolver _clientResolver;
    private readonly IFieldSecurityService _fieldSecurity;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AccessPolicyHitQueryService(ISqlSugarClientResolver clientResolver, IFieldSecurityService fieldSecurity)
    {
        _clientResolver = clientResolver;
        _fieldSecurity = fieldSecurity;
    }

    /// <summary>
    /// 命中记录分页列表
    /// </summary>
    [PermissionAuthorize(SaasPermissionCodes.AccessPolicyHit.Read)]
    [HttpPost]
    public async Task<PageResultDtoBase<AccessPolicyHitListItemDto>> GetAccessPolicyHitPageAsync(AccessPolicyHitPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();
        if (input.HitTimeStart.HasValue && input.HitTimeEnd.HasValue && input.HitTimeStart.Value > input.HitTimeEnd.Value)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "命中开始时间不能晚于结束时间。");
        }

        await _fieldSecurity.GuardQueryAsync(input.Conditions, typeof(SysAccessPolicyHit), cancellationToken);
        var start = input.HitTimeStart.GetValueOrDefault();
        var end = input.HitTimeEnd.GetValueOrDefault();
        var source = input.Source.GetValueOrDefault();
        var policyId = input.PolicyId.GetValueOrDefault();
        var action = input.Action.GetValueOrDefault();
        var phase = input.Phase.GetValueOrDefault();
        var isBlocked = input.IsBlocked.GetValueOrDefault();
        var policyCode = input.PolicyCode?.Trim();
        var ip = input.Ip?.Trim();
        var userName = input.UserName?.Trim();
        var path = input.Path?.Trim();
        var traceId = input.TraceId?.Trim();
        var query = _clientResolver.GetCurrentClient().Queryable<SysAccessPolicyHit>()
            .WhereIF(input.HitTimeStart.HasValue, hit => hit.HitTime >= start)
            .WhereIF(input.HitTimeEnd.HasValue, hit => hit.HitTime <= end)
            .WhereIF(input.Source.HasValue, hit => hit.Source == source)
            .WhereIF(input.PolicyId.HasValue, hit => hit.PolicyId == policyId)
            .WhereIF(!string.IsNullOrEmpty(policyCode), hit => hit.PolicyCode.Contains(policyCode!))
            .WhereIF(input.Action.HasValue, hit => hit.Action == action)
            .WhereIF(input.Phase.HasValue, hit => hit.Phase == phase)
            .WhereIF(input.IsBlocked.HasValue, hit => hit.IsBlocked == isBlocked)
            .WhereIF(!string.IsNullOrEmpty(ip), hit => hit.Ip != null && hit.Ip.Contains(ip!))
            .WhereIF(!string.IsNullOrEmpty(userName), hit => hit.UserName != null && hit.UserName.Contains(userName!))
            .WhereIF(!string.IsNullOrEmpty(path), hit => hit.Path != null && hit.Path.Contains(path!))
            .WhereIF(!string.IsNullOrEmpty(traceId), hit => hit.TraceId == traceId)
            .SplitTable();
        query = query.ApplyFilters(input.Conditions.Filters);
        query = input.Conditions.Sorts.Count > 0
            ? query.ApplySorts(input.Conditions.Sorts)
            : query.OrderByDescending(hit => hit.HitTime);

        RefAsync<int> totalCount = 0;
        var entities = await query.ToPageListAsync(input.Page.PageIndex, input.Page.PageSize, totalCount, cancellationToken);
        var page = new PageResultMetadata(input.Page.PageIndex, input.Page.PageSize, totalCount);
        return new PageResultDtoBase<AccessPolicyHitListItemDto>([.. entities.Select(AccessPolicyApplicationMapper.ToHitListItemDto)], page);
    }

    /// <summary>
    /// 命中记录详情
    /// </summary>
    [PermissionAuthorize(SaasPermissionCodes.AccessPolicyHit.Read)]
    public async Task<AccessPolicyHitListItemDto?> GetAccessPolicyHitDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "命中记录主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();
        var hit = await _clientResolver.GetCurrentClient().Queryable<SysAccessPolicyHit>()
            .Where(item => item.BasicId == id)
            .SplitTable()
            .FirstAsync(cancellationToken);
        return hit is null ? null : AccessPolicyApplicationMapper.ToHitListItemDto(hit);
    }
}
