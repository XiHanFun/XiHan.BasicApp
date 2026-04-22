#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FieldSecurityService
// Guid:cc1dc10c-803e-46ab-a6bc-e0c65fe88040
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/22 22:14:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Application.InternalServices.Implementations;

public class FieldSecurityService : IFieldSecurityService, IScopedDependency
{
    private readonly IAuthorizationContextService _authorizationContextService;
    private readonly IRoleResolutionDomainService _roleResolutionDomainService;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IFieldLevelSecurityRepository _fieldLevelSecurityRepository;
    private readonly IFieldSecurityCacheService _fieldSecurityCacheService;
    private readonly ISqlSugarClientResolver _clientResolver;

    private ISqlSugarClient DbClient => _clientResolver.GetCurrentClient();

    public FieldSecurityService(
        IAuthorizationContextService authorizationContextService,
        IRoleResolutionDomainService roleResolutionDomainService,
        IPermissionRepository permissionRepository,
        IFieldLevelSecurityRepository fieldLevelSecurityRepository,
        IFieldSecurityCacheService fieldSecurityCacheService,
        ISqlSugarClientResolver clientResolver)
    {
        _authorizationContextService = authorizationContextService;
        _roleResolutionDomainService = roleResolutionDomainService;
        _permissionRepository = permissionRepository;
        _fieldLevelSecurityRepository = fieldLevelSecurityRepository;
        _fieldSecurityCacheService = fieldSecurityCacheService;
        _clientResolver = clientResolver;
    }

    public async Task<FieldSecurityDecisionDto> GetCurrentUserFieldSecurityAsync(
        string resourceCode,
        IReadOnlyCollection<string>? fieldNames = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(resourceCode))
        {
            throw new ArgumentException("资源编码不能为空。", nameof(resourceCode));
        }

        var session = await _authorizationContextService.GetCurrentContextAsync(cancellationToken)
                      ?? throw new InvalidOperationException("当前用户未登录。");
        var normalizedResourceCode = resourceCode.Trim();
        var normalizedFields = NormalizeFields(fieldNames);
        var effectiveTenantId = session.CurrentTenantId ?? (session.User.TenantId > 0 ? session.User.TenantId : null);

        var resource = await ResolveResourceAsync(normalizedResourceCode, effectiveTenantId, cancellationToken)
                       ?? throw new KeyNotFoundException($"未找到资源编码 {normalizedResourceCode}。");

        var cacheItem = await _fieldSecurityCacheService.GetDecisionAsync(
            session.User.BasicId,
            effectiveTenantId,
            normalizedResourceCode,
            normalizedFields,
            async token => await BuildDecisionCacheItemAsync(session.User, effectiveTenantId, resource, normalizedFields, token),
            cancellationToken);

        return new FieldSecurityDecisionDto
        {
            ResourceCode = resource.ResourceCode,
            ResourceId = resource.BasicId,
            Fields = cacheItem?.Fields
                .Select(static item => new FieldSecurityFieldDecisionDto
                {
                    FieldName = item.FieldName,
                    IsReadable = item.IsReadable,
                    IsEditable = item.IsEditable,
                    MaskStrategy = (FieldMaskStrategy)item.MaskStrategy,
                    MaskPattern = item.MaskPattern,
                    Priority = item.Priority
                })
                .OrderBy(static item => item.FieldName, StringComparer.Ordinal)
                .ToArray() ?? []
        };
    }

    private async Task<FieldSecurityDecisionCacheItem> BuildDecisionCacheItemAsync(
        SysUser user,
        long? tenantId,
        SysResource resource,
        IReadOnlyCollection<string> fieldNames,
        CancellationToken cancellationToken)
    {
        var roleIds = await _roleResolutionDomainService.GetEffectiveRoleIdsAsync(user.BasicId, tenantId, cancellationToken);
        var permissions = await _permissionRepository.GetUserPermissionsAsync(user.BasicId, tenantId, cancellationToken);
        var permissionIds = permissions
            .Where(static item => item.Status == YesOrNo.Yes)
            .Select(static item => item.BasicId)
            .Distinct()
            .ToArray();

        var rules = await _fieldLevelSecurityRepository.GetEffectiveRulesAsync(
            user.BasicId,
            tenantId,
            resource.BasicId,
            roleIds,
            permissionIds,
            fieldNames,
            cancellationToken);

        return new FieldSecurityDecisionCacheItem
        {
            Fields = rules
                .GroupBy(static rule => rule.FieldName, StringComparer.Ordinal)
                .Select(group => MergeFieldRules(group.Key, group))
                .OrderBy(static item => item.FieldName, StringComparer.Ordinal)
                .ToArray()
        };
    }

    private async Task<SysResource?> ResolveResourceAsync(string resourceCode, long? tenantId, CancellationToken cancellationToken)
    {
        var resources = await DbClient.Queryable<SysResource>()
            .Where(resource =>
                resource.ResourceCode == resourceCode
                && resource.Status == YesOrNo.Yes
                && !resource.IsDeleted
                && (resource.TenantId == 0 || (tenantId.HasValue && resource.TenantId == tenantId.Value)))
            .OrderByDescending(resource => resource.TenantId)
            .ToListAsync(cancellationToken);

        return resources.FirstOrDefault();
    }

    private static FieldSecurityDecisionFieldCacheItem MergeFieldRules(string fieldName, IEnumerable<SysFieldLevelSecurity> rules)
    {
        var orderedRules = rules
            .OrderByDescending(static rule => rule.Priority)
            .ThenByDescending(static rule => GetTargetOrder(rule.TargetType))
            .ToArray();

        var highestPriority = orderedRules[0].Priority;
        var topRules = orderedRules.Where(rule => rule.Priority == highestPriority).ToArray();
        var maskRule = topRules.FirstOrDefault(static rule => !rule.IsReadable && rule.MaskStrategy != FieldMaskStrategy.None)
                       ?? topRules.FirstOrDefault(static rule => !rule.IsReadable)
                       ?? topRules[0];

        return new FieldSecurityDecisionFieldCacheItem
        {
            FieldName = fieldName,
            IsReadable = topRules.All(static rule => rule.IsReadable),
            IsEditable = topRules.All(static rule => rule.IsEditable),
            MaskStrategy = (int)maskRule.MaskStrategy,
            MaskPattern = maskRule.MaskPattern,
            Priority = highestPriority
        };
    }

    private static IReadOnlyCollection<string> NormalizeFields(IReadOnlyCollection<string>? fieldNames)
    {
        return fieldNames?
            .Where(static field => !string.IsNullOrWhiteSpace(field))
            .Select(static field => field.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray() ?? [];
    }

    private static int GetTargetOrder(FieldSecurityTargetType targetType)
    {
        return targetType switch
        {
            FieldSecurityTargetType.User => 3,
            FieldSecurityTargetType.Permission => 2,
            _ => 1
        };
    }
}
