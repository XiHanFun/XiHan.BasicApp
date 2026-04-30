#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ConstraintRuleQueryService
// Guid:73a9df28-2db7-4f90-a13c-188c1d4973d6
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Enums;
using XiHan.Framework.Domain.Shared.Paging.Models;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 约束规则查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "约束规则")]
public sealed class ConstraintRuleQueryService(
    IConstraintRuleRepository constraintRuleRepository,
    IConstraintRuleItemRepository constraintRuleItemRepository,
    IRoleRepository roleRepository,
    IPermissionRepository permissionRepository,
    ITenantUserRepository tenantUserRepository)
    : SaasApplicationService, IConstraintRuleQueryService
{
    /// <summary>
    /// 约束规则仓储
    /// </summary>
    private readonly IConstraintRuleRepository _constraintRuleRepository = constraintRuleRepository;

    /// <summary>
    /// 约束规则项仓储
    /// </summary>
    private readonly IConstraintRuleItemRepository _constraintRuleItemRepository = constraintRuleItemRepository;

    /// <summary>
    /// 角色仓储
    /// </summary>
    private readonly IRoleRepository _roleRepository = roleRepository;

    /// <summary>
    /// 权限仓储
    /// </summary>
    private readonly IPermissionRepository _permissionRepository = permissionRepository;

    /// <summary>
    /// 租户成员仓储
    /// </summary>
    private readonly ITenantUserRepository _tenantUserRepository = tenantUserRepository;

    /// <summary>
    /// 获取约束规则分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>约束规则分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.ConstraintRule.Read)]
    public async Task<PageResultDtoBase<ConstraintRuleListItemDto>> GetConstraintRulePageAsync(ConstraintRulePageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildConstraintRulePageRequest(input);
        var rules = await _constraintRuleRepository.GetPagedAsync(request, cancellationToken);
        if (rules.Items.Count == 0)
        {
            return new PageResultDtoBase<ConstraintRuleListItemDto>([], rules.Page);
        }

        var itemCountMap = await BuildItemCountMapAsync(rules.Items.Select(rule => rule.BasicId), cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var items = rules.Items
            .Select(rule => ConstraintRuleApplicationMapper.ToListItemDto(
                rule,
                itemCountMap.GetValueOrDefault(rule.BasicId),
                now))
            .ToList();

        return new PageResultDtoBase<ConstraintRuleListItemDto>(items, rules.Page);
    }

    /// <summary>
    /// 获取约束规则详情
    /// </summary>
    /// <param name="id">约束规则主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>约束规则详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.ConstraintRule.Read)]
    public async Task<ConstraintRuleDetailDto?> GetConstraintRuleDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "约束规则主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var rule = await _constraintRuleRepository.GetByIdAsync(id, cancellationToken);
        if (rule is null)
        {
            return null;
        }

        var items = await _constraintRuleItemRepository.GetListAsync(
            item => item.ConstraintRuleId == id,
            item => item.ConstraintGroup,
            cancellationToken);
        var targetSummaryMap = await BuildTargetSummaryMapAsync(items, cancellationToken);
        var itemDtos = items
            .Select(item =>
            {
                var summary = targetSummaryMap.GetValueOrDefault((item.TargetType, item.TargetId));
                return ConstraintRuleApplicationMapper.ToItemDto(item, summary.Code, summary.Name);
            })
            .ToList();

        return ConstraintRuleApplicationMapper.ToDetailDto(rule, itemDtos, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// 构建约束规则分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>约束规则分页请求</returns>
    private static BasicAppPRDto BuildConstraintRulePageRequest(ConstraintRulePageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Behavior = input.Behavior,
            Conditions = new QueryConditions()
        };

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword(
                input.Keyword.Trim(),
                nameof(SysConstraintRule.RuleCode),
                nameof(SysConstraintRule.RuleName),
                nameof(SysConstraintRule.Description),
                nameof(SysConstraintRule.Remark));
        }

        if (input.ConstraintType.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysConstraintRule.ConstraintType), input.ConstraintType.Value);
        }

        if (input.TargetType.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysConstraintRule.TargetType), input.TargetType.Value);
        }

        if (input.ViolationAction.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysConstraintRule.ViolationAction), input.ViolationAction.Value);
        }

        if (input.IsGlobal.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysConstraintRule.IsGlobal), input.IsGlobal.Value);
        }

        if (input.Status.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysConstraintRule.Status), input.Status.Value);
        }

        request.Conditions.AddSort(nameof(SysConstraintRule.Priority), SortDirection.Descending, 0);
        request.Conditions.AddSort(nameof(SysConstraintRule.CreatedTime), SortDirection.Descending, 1);
        return request;
    }

    /// <summary>
    /// 构建规则项数量映射
    /// </summary>
    private async Task<IReadOnlyDictionary<long, int>> BuildItemCountMapAsync(IEnumerable<long> ruleIds, CancellationToken cancellationToken)
    {
        var ids = ruleIds
            .Where(ruleId => ruleId > 0)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<long, int>();
        }

        var items = await _constraintRuleItemRepository.GetListAsync(
            item => ids.Contains(item.ConstraintRuleId),
            item => item.CreatedTime,
            cancellationToken);
        return items
            .GroupBy(item => item.ConstraintRuleId)
            .ToDictionary(group => group.Key, group => group.Count());
    }

    /// <summary>
    /// 构建目标摘要映射
    /// </summary>
    private async Task<IReadOnlyDictionary<(ConstraintTargetType Type, long Id), (string? Code, string? Name)>> BuildTargetSummaryMapAsync(
        IReadOnlyList<SysConstraintRuleItem> items,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<(ConstraintTargetType Type, long Id), (string? Code, string? Name)>();
        await AddRoleSummariesAsync(items, result, cancellationToken);
        await AddPermissionSummariesAsync(items, result, cancellationToken);
        await AddTenantMemberSummariesAsync(items, result, cancellationToken);
        return result;
    }

    /// <summary>
    /// 添加角色摘要
    /// </summary>
    private async Task AddRoleSummariesAsync(
        IReadOnlyList<SysConstraintRuleItem> items,
        IDictionary<(ConstraintTargetType Type, long Id), (string? Code, string? Name)> result,
        CancellationToken cancellationToken)
    {
        var ids = items
            .Where(item => item.TargetType == ConstraintTargetType.Role && item.TargetId > 0)
            .Select(item => item.TargetId)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return;
        }

        var roles = await _roleRepository.GetByIdsAsync(ids, cancellationToken);
        foreach (var role in roles)
        {
            result[(ConstraintTargetType.Role, role.BasicId)] = (role.RoleCode, role.RoleName);
        }
    }

    /// <summary>
    /// 添加权限摘要
    /// </summary>
    private async Task AddPermissionSummariesAsync(
        IReadOnlyList<SysConstraintRuleItem> items,
        IDictionary<(ConstraintTargetType Type, long Id), (string? Code, string? Name)> result,
        CancellationToken cancellationToken)
    {
        var ids = items
            .Where(item => item.TargetType == ConstraintTargetType.Permission && item.TargetId > 0)
            .Select(item => item.TargetId)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return;
        }

        var permissions = await _permissionRepository.GetByIdsAsync(ids, cancellationToken);
        foreach (var permission in permissions)
        {
            result[(ConstraintTargetType.Permission, permission.BasicId)] = (permission.PermissionCode, permission.PermissionName);
        }
    }

    /// <summary>
    /// 添加租户成员摘要
    /// </summary>
    private async Task AddTenantMemberSummariesAsync(
        IReadOnlyList<SysConstraintRuleItem> items,
        IDictionary<(ConstraintTargetType Type, long Id), (string? Code, string? Name)> result,
        CancellationToken cancellationToken)
    {
        var userIds = items
            .Where(item => item.TargetType == ConstraintTargetType.User && item.TargetId > 0)
            .Select(item => item.TargetId)
            .Distinct()
            .ToArray();

        if (userIds.Length == 0)
        {
            return;
        }

        var tenantMembers = await _tenantUserRepository.GetListAsync(
            tenantMember => userIds.Contains(tenantMember.UserId),
            tenantMember => tenantMember.CreatedTime,
            cancellationToken);
        foreach (var tenantMember in tenantMembers)
        {
            result[(ConstraintTargetType.User, tenantMember.UserId)] = (null, tenantMember.DisplayName);
        }
    }
}
