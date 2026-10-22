// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Domain.Conditions;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Authorization.Conditions;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Application.Conditions;

/// <summary>
/// 权限条件盘点：逐条诊断存量条件，按组给出生效后的结论
/// </summary>
/// <remarks>
/// 条件都在平台库，平台上下文扫全部租户，租户上下文只看本租户（含它能读到的全局角色上的条件）。
/// 每个租户的授权行在各自的库里，读绑定时切到该租户。
/// </remarks>
public sealed class PermissionConditionInventoryService
{
    private readonly IPermissionConditionRepository _conditionRepository;
    private readonly IRolePermissionRepository _rolePermissionRepository;
    private readonly IUserPermissionRepository _userPermissionRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly ICurrentTenant _currentTenant;
    private readonly SaasConditionSetCompiler _compiler;
    private readonly IConditionAttributeRegistry _attributes;
    private readonly IPermissionResourceRegistry _resources;

    /// <summary>
    /// 构造函数
    /// </summary>
    public PermissionConditionInventoryService(
        IPermissionConditionRepository conditionRepository,
        IRolePermissionRepository rolePermissionRepository,
        IUserPermissionRepository userPermissionRepository,
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IUserRepository userRepository,
        ITenantRepository tenantRepository,
        ICurrentTenant currentTenant,
        SaasConditionSetCompiler compiler,
        IConditionAttributeRegistry attributes,
        IPermissionResourceRegistry resources)
    {
        _conditionRepository = conditionRepository;
        _rolePermissionRepository = rolePermissionRepository;
        _userPermissionRepository = userPermissionRepository;
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _userRepository = userRepository;
        _tenantRepository = tenantRepository;
        _currentTenant = currentTenant;
        _compiler = compiler;
        _attributes = attributes;
        _resources = resources;
    }

    /// <summary>
    /// 各类条数写成「名称 条数」逗号连接；一条都没有时为「无」
    /// </summary>
    public static string FormatCounts<TKey>(IReadOnlyDictionary<TKey, int> counts)
        where TKey : notnull
    {
        return counts.Count == 0 ? "无" : string.Join("，", counts.Select(item => $"{item.Key} {item.Value}"));
    }

    /// <summary>
    /// 生成盘点
    /// </summary>
    public async Task<PermissionConditionInventoryDto> BuildAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var isPlatform = _currentTenant.Id is not > 0;
        var conditions = await _conditionRepository.GetForInventoryAsync(isPlatform, cancellationToken);
        var tenantIds = conditions.Select(condition => condition.TenantId).Where(id => id > 0).Distinct().ToList();
        var tenantCodes = tenantIds.Count == 0
            ? new Dictionary<long, string>()
            : (await _tenantRepository.GetByIdsAsync(tenantIds, cancellationToken)).ToDictionary(tenant => tenant.BasicId, tenant => tenant.TenantCode);

        var items = new List<PermissionConditionInventoryItemDto>(conditions.Count);
        foreach (var tenantGroup in conditions.GroupBy(condition => condition.TenantId))
        {
            var target = tenantGroup.Key > 0 ? tenantGroup.Key : (long?)null;
            using var _ = isPlatform ? _currentTenant.Change(target, target?.ToString()) : null;
            var bindings = await LoadBindingsAsync([.. tenantGroup], cancellationToken);
            items.AddRange(tenantGroup.Select(condition => Diagnose(condition, bindings, now, tenantCodes.GetValueOrDefault(condition.TenantId))));
        }

        ConcludeGroups(items);
        return new PermissionConditionInventoryDto
        {
            GeneratedTime = now,
            ConditionCount = items.Count,
            BindingCount = items.Select(BindingKey).Distinct(StringComparer.Ordinal).Count(),
            GroupConclusions = items
                .GroupBy(item => (BindingKey(item), item.ConditionGroup))
                .GroupBy(group => group.First().GroupConclusion)
                .ToDictionary(group => group.Key, group => group.Count()),
            Diagnostics = items
                .SelectMany(item => item.Diagnostics)
                .GroupBy(diagnostic => diagnostic)
                .ToDictionary(group => group.Key, group => group.Count()),
            Items = [.. items
                .OrderBy(item => item.TenantId)
                .ThenBy(item => BindingKey(item), StringComparer.Ordinal)
                .ThenBy(item => item.ConditionGroup)
                .ThenBy(item => item.ConditionId)]
        };
    }

    private static string BindingKey(PermissionConditionInventoryItemDto item)
    {
        return item.BindingKind switch
        {
            PermissionConditionBindingKind.Role => $"rp:{item.RolePermissionId}",
            PermissionConditionBindingKind.User => $"up:{item.UserPermissionId}",
            _ => $"c:{item.ConditionId}"
        };
    }

    /// <summary>
    /// 组的结论：全部停用不参与；授权失效不影响任何人；有启用的子句恒不命中则这组恒不满足；否则可正常求值
    /// </summary>
    private static void ConcludeGroups(List<PermissionConditionInventoryItemDto> items)
    {
        foreach (var group in items.GroupBy(item => (BindingKey(item), item.ConditionGroup)))
        {
            var enabled = group.Where(item => item.Status == ValidityStatus.Valid).ToList();
            var conclusion = enabled.Count == 0
                ? PermissionConditionGroupConclusion.Disabled
                : group.Any(item => item.Diagnostics.Contains(PermissionConditionDiagnostic.BindingInactive) || item.Diagnostics.Contains(PermissionConditionDiagnostic.AmbiguousBinding))
                    ? PermissionConditionGroupConclusion.BindingInactive
                    : enabled.Any(item => item.Diagnostics.Contains(PermissionConditionDiagnostic.UnregisteredAttribute) || item.Diagnostics.Contains(PermissionConditionDiagnostic.InvalidCondition))
                        ? PermissionConditionGroupConclusion.NeverMatches
                        : PermissionConditionGroupConclusion.Evaluable;
            foreach (var item in group)
            {
                item.GroupConclusion = conclusion;
            }
        }
    }

    private PermissionConditionInventoryItemDto Diagnose(SysPermissionCondition condition, Bindings bindings, DateTimeOffset now, string? tenantCode)
    {
        var item = new PermissionConditionInventoryItemDto
        {
            ConditionId = condition.BasicId,
            TenantId = condition.TenantId,
            TenantCode = tenantCode,
            RolePermissionId = condition.RolePermissionId,
            UserPermissionId = condition.UserPermissionId,
            ConditionGroup = condition.ConditionGroup,
            AttributeName = condition.AttributeName,
            Operator = condition.Operator,
            IsNegated = condition.IsNegated,
            ValueType = condition.ValueType,
            ConditionValue = condition.ConditionValue,
            Status = condition.Status
        };

        long? permissionId = null;
        var bindingActive = false;
        if (condition.RolePermissionId.HasValue == condition.UserPermissionId.HasValue)
        {
            item.BindingKind = PermissionConditionBindingKind.Ambiguous;
            item.Diagnostics.Add(PermissionConditionDiagnostic.AmbiguousBinding);
        }
        else if (condition.RolePermissionId is { } rolePermissionId)
        {
            item.BindingKind = PermissionConditionBindingKind.Role;
            var rolePermission = bindings.RolePermissions.GetValueOrDefault(rolePermissionId);
            var role = rolePermission is null ? null : bindings.Roles.GetValueOrDefault(rolePermission.RoleId);
            permissionId = rolePermission?.PermissionId;
            item.PermissionAction = rolePermission?.PermissionAction;
            item.RoleCode = role?.RoleCode;
            item.RoleName = role?.RoleName;
            bindingActive = rolePermission is not null
                && IsInPeriod(rolePermission.Status, rolePermission.EffectiveTime, rolePermission.ExpirationTime, now)
                && role?.Status == EnableStatus.Enabled;
        }
        else
        {
            item.BindingKind = PermissionConditionBindingKind.User;
            var userPermission = bindings.UserPermissions.GetValueOrDefault(condition.UserPermissionId!.Value);
            permissionId = userPermission?.PermissionId;
            item.PermissionAction = userPermission?.PermissionAction;
            item.UserId = userPermission?.UserId;
            item.UserName = userPermission is null ? null : bindings.Users.GetValueOrDefault(userPermission.UserId)?.UserName;
            bindingActive = userPermission is not null
                && IsInPeriod(userPermission.Status, userPermission.EffectiveTime, userPermission.ExpirationTime, now);
        }

        var permission = permissionId is { } id ? bindings.Permissions.GetValueOrDefault(id) : null;
        item.PermissionCode = permission?.PermissionCode;
        item.PermissionName = permission?.PermissionName;
        if (item.BindingKind != PermissionConditionBindingKind.Ambiguous && (!bindingActive || permission?.Status != EnableStatus.Enabled))
        {
            item.Diagnostics.Add(PermissionConditionDiagnostic.BindingInactive);
        }

        var permissionCode = item.PermissionCode ?? string.Empty;
        var resolved = _attributes.TryResolve(condition.AttributeName, out var resolution)
            || (_resources.TryGet(permissionCode, out var registration) && registration.IsRowAttribute(condition.AttributeName.Trim()));
        if (!resolved)
        {
            item.Diagnostics.Add(PermissionConditionDiagnostic.UnregisteredAttribute);
            item.Message = condition.AttributeName.Trim().StartsWith("resource.", StringComparison.OrdinalIgnoreCase)
                ? "数据行属性未按权限码登记"
                : "属性未登记";
            return item;
        }

        if (resolution is not null && SaasConditionAttributes.IsLegacy(resolution))
        {
            item.Diagnostics.Add(PermissionConditionDiagnostic.LegacyName);
        }

        var compiled = _compiler.CompileClauses(permissionCode,
        [
            new AuthorizationConditionClause
            {
                Id = condition.BasicId,
                AttributeName = condition.AttributeName,
                Operator = (int)condition.Operator,
                IsNegated = condition.IsNegated,
                Value = condition.ConditionValue
            }
        ]);
        if (compiled.Errors.Count > 0)
        {
            item.Diagnostics.Add(PermissionConditionDiagnostic.InvalidCondition);
            item.Message = compiled.Errors[0].Message;
            return item;
        }

        var kind = compiled.Groups[0].Clauses[0].Resolution!.Descriptor.Kind;
        if ((condition.ValueType == ConfigDataType.Boolean && kind != ConditionValueKind.Boolean)
            || (condition.ValueType == ConfigDataType.Number && kind != ConditionValueKind.Number))
        {
            item.Diagnostics.Add(PermissionConditionDiagnostic.ValueTypeMismatch);
        }

        return item;
    }

    private static bool IsInPeriod(ValidityStatus status, DateTimeOffset? effectiveTime, DateTimeOffset? expirationTime, DateTimeOffset now)
    {
        return status == ValidityStatus.Valid
            && (effectiveTime is null || effectiveTime <= now)
            && (expirationTime is null || expirationTime > now);
    }

    private async Task<Bindings> LoadBindingsAsync(IReadOnlyList<SysPermissionCondition> conditions, CancellationToken cancellationToken)
    {
        var rolePermissionIds = conditions.Where(condition => condition.RolePermissionId is > 0).Select(condition => condition.RolePermissionId!.Value).Distinct().ToList();
        var userPermissionIds = conditions.Where(condition => condition.UserPermissionId is > 0).Select(condition => condition.UserPermissionId!.Value).Distinct().ToList();
        var rolePermissions = rolePermissionIds.Count == 0 ? [] : await _rolePermissionRepository.GetByIdsAsync(rolePermissionIds, cancellationToken);
        var userPermissions = userPermissionIds.Count == 0 ? [] : await _userPermissionRepository.GetByIdsAsync(userPermissionIds, cancellationToken);
        var roleIds = rolePermissions.Select(row => row.RoleId).Distinct().ToList();
        var permissionIds = rolePermissions.Select(row => row.PermissionId).Concat(userPermissions.Select(row => row.PermissionId)).Distinct().ToList();
        var userIds = userPermissions.Select(row => row.UserId).Distinct().ToList();
        return new Bindings(
            rolePermissions.ToDictionary(row => row.BasicId),
            userPermissions.ToDictionary(row => row.BasicId),
            roleIds.Count == 0 ? [] : (await _roleRepository.GetByIdsAsync(roleIds, cancellationToken)).ToDictionary(role => role.BasicId),
            permissionIds.Count == 0 ? [] : (await _permissionRepository.GetByIdsAsync(permissionIds, cancellationToken)).ToDictionary(permission => permission.BasicId),
            userIds.Count == 0 ? [] : (await _userRepository.GetListByIdsIgnoreTenantAsync(userIds, cancellationToken)).ToDictionary(user => user.BasicId));
    }

    private sealed record Bindings(
        IReadOnlyDictionary<long, SysRolePermission> RolePermissions,
        IReadOnlyDictionary<long, SysUserPermission> UserPermissions,
        IReadOnlyDictionary<long, SysRole> Roles,
        IReadOnlyDictionary<long, SysPermission> Permissions,
        IReadOnlyDictionary<long, SysUser> Users);
}
