#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasTenantEditionSeeder
// Guid:2d07a11b-26fd-4e0e-84d0-3a5b7c9a9f10
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/04 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using SqlSugar;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders.System;

/// <summary>
/// SaaS 租户版本和版本权限种子数据
/// </summary>
public sealed class SaasTenantEditionSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<SaasTenantEditionSeeder> logger,
    IServiceProvider serviceProvider,
    ICurrentTenant currentTenant)
    : DataSeederBase(clientResolver, logger, serviceProvider)
{
    private const string DefaultTenantCode = "default";
    private const string FreeEditionCode = "free";
    private const string EnterpriseEditionCode = "enterprise";

    private readonly ICurrentTenant _currentTenant = currentTenant;

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 21;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[SaaS]租户版本种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        using var platformScope = _currentTenant.Change(null);
        var client = DbClient;
        var defaultEditionCode = await ResolveDefaultEditionCodeAsync(client);
        var definitions = BuildDefinitions(defaultEditionCode);
        var editionResult = await EnsureEditionsAsync(client, definitions);

        var permissions = await client.Queryable<SysPermission>()
            .Where(permission => permission.TenantId == 0 && permission.Status == EnableStatus.Enabled)
            .ToListAsync();
        if (permissions.Count == 0)
        {
            Logger.LogWarning("平台全局权限不存在，跳过租户版本权限种子数据");
            return;
        }

        var bindingResult = await EnsureEditionPermissionsAsync(client, definitions, editionResult.Editions, permissions);
        var tenantUpdated = await EnsureDefaultTenantEditionAsync(client, editionResult.Editions);

        if (editionResult.AddCount == 0
            && editionResult.UpdateCount == 0
            && bindingResult.AddCount == 0
            && bindingResult.UpdateCount == 0
            && !tenantUpdated)
        {
            Logger.LogInformation("SaaS 租户版本数据已存在，跳过种子数据");
            return;
        }

        Logger.LogInformation(
            "成功初始化 SaaS 租户版本，版本新增 {EditionAddCount} 个，版本更新 {EditionUpdateCount} 个，权限新增 {PermissionAddCount} 个，权限更新 {PermissionUpdateCount} 个",
            editionResult.AddCount,
            editionResult.UpdateCount,
            bindingResult.AddCount,
            bindingResult.UpdateCount);
    }

    private static async Task<string> ResolveDefaultEditionCodeAsync(ISqlSugarClient client)
    {
        var enabledDefaults = await client.Queryable<SysTenantEdition>()
            .Where(edition => edition.IsDefault && edition.Status == EnableStatus.Enabled)
            .ToListAsync();

        return enabledDefaults
            .OrderBy(edition => edition.Sort)
            .ThenBy(edition => edition.BasicId)
            .Select(edition => edition.EditionCode)
            .FirstOrDefault(code => !string.IsNullOrWhiteSpace(code))
            ?? FreeEditionCode;
    }

    private static async Task<EditionSeedResult> EnsureEditionsAsync(ISqlSugarClient client, IReadOnlyList<EditionSeedDefinition> definitions)
    {
        var editionCodes = definitions.Select(definition => definition.EditionCode).ToArray();
        var existingEditions = await client.Queryable<SysTenantEdition>()
            .Where(edition => editionCodes.Contains(edition.EditionCode))
            .ToListAsync();
        var editionMap = existingEditions
            .GroupBy(edition => edition.EditionCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First(),
                StringComparer.OrdinalIgnoreCase);

        var addCount = 0;
        var updateCount = 0;
        foreach (var definition in definitions)
        {
            if (editionMap.TryGetValue(definition.EditionCode, out var existing))
            {
                if (ApplyEditionDefinition(existing, definition))
                {
                    _ = await client.Updateable(existing).ExecuteCommandAsync();
                    updateCount++;
                }

                continue;
            }

            var edition = CreateEdition(definition);
            var savedEdition = await client.Insertable(edition).ExecuteReturnEntityAsync();
            editionMap[definition.EditionCode] = savedEdition;
            addCount++;
        }

        return new EditionSeedResult(editionMap, addCount, updateCount);
    }

    private static async Task<BindingSeedResult> EnsureEditionPermissionsAsync(
        ISqlSugarClient client,
        IReadOnlyList<EditionSeedDefinition> definitions,
        IReadOnlyDictionary<string, SysTenantEdition> editionMap,
        IReadOnlyList<SysPermission> permissions)
    {
        var editionIds = editionMap.Values.Select(edition => edition.BasicId).ToArray();
        var permissionIds = permissions.Select(permission => permission.BasicId).ToArray();
        var existingBindings = await client.Queryable<SysTenantEditionPermission>()
            .Where(binding => editionIds.Contains(binding.EditionId) && permissionIds.Contains(binding.PermissionId))
            .ToListAsync();
        var bindingMap = existingBindings.ToDictionary(binding => (binding.EditionId, binding.PermissionId));

        var permissionMap = permissions
            .GroupBy(permission => permission.PermissionCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First(),
                StringComparer.OrdinalIgnoreCase);

        var addList = new List<SysTenantEditionPermission>();
        var updateCount = 0;
        foreach (var definition in definitions)
        {
            if (!editionMap.TryGetValue(definition.EditionCode, out var edition))
            {
                continue;
            }

            var permissionCodes = ResolvePermissionCodes(definition, permissions);
            foreach (var permissionCode in permissionCodes)
            {
                if (!permissionMap.TryGetValue(permissionCode, out var permission))
                {
                    continue;
                }

                if (bindingMap.TryGetValue((edition.BasicId, permission.BasicId), out var existing))
                {
                    if (ApplyBindingDefinition(existing, edition.BasicId, permission.BasicId, definition.EditionName))
                    {
                        _ = await client.Updateable(existing).ExecuteCommandAsync();
                        updateCount++;
                    }

                    continue;
                }

                addList.Add(CreateBinding(edition.BasicId, permission.BasicId, definition.EditionName));
            }
        }

        if (addList.Count > 0)
        {
            await client.Insertable(addList).ExecuteReturnSnowflakeIdListAsync();
        }

        return new BindingSeedResult(addList.Count, updateCount);
    }

    private static async Task<bool> EnsureDefaultTenantEditionAsync(
        ISqlSugarClient client,
        IReadOnlyDictionary<string, SysTenantEdition> editionMap)
    {
        var defaultTenant = await client.Queryable<SysTenant>()
            .FirstAsync(tenant => tenant.TenantCode == DefaultTenantCode);
        if (defaultTenant is null || defaultTenant.EditionId.HasValue)
        {
            return false;
        }

        var edition = editionMap.TryGetValue(EnterpriseEditionCode, out var enterpriseEdition)
            ? enterpriseEdition
            : editionMap.Values
                .OrderByDescending(item => item.IsDefault)
                .ThenBy(item => item.Sort)
                .FirstOrDefault();
        if (edition is null)
        {
            return false;
        }

        defaultTenant.EditionId = edition.BasicId;
        _ = await client.Updateable(defaultTenant).ExecuteCommandAsync();
        return true;
    }

    private static SysTenantEdition CreateEdition(EditionSeedDefinition definition)
    {
        var edition = new SysTenantEdition
        {
            EditionCode = definition.EditionCode
        };
        _ = ApplyEditionDefinition(edition, definition);
        return edition;
    }

    private static bool ApplyEditionDefinition(SysTenantEdition edition, EditionSeedDefinition definition)
    {
        var changed = false;
        changed |= SetIfChanged(edition.TenantId, 0, value => edition.TenantId = value);
        changed |= SetIfChanged(edition.EditionCode, definition.EditionCode, value => edition.EditionCode = value);
        changed |= SetIfChanged(edition.EditionName, definition.EditionName, value => edition.EditionName = value);
        changed |= SetIfChanged(edition.Description, definition.Description, value => edition.Description = value);
        changed |= SetIfChanged(edition.UserLimit, definition.UserLimit, value => edition.UserLimit = value);
        changed |= SetIfChanged(edition.StorageLimit, definition.StorageLimit, value => edition.StorageLimit = value);
        changed |= SetIfChanged(edition.Price, definition.Price, value => edition.Price = value);
        changed |= SetIfChanged(edition.BillingPeriodMonths, definition.BillingPeriodMonths, value => edition.BillingPeriodMonths = value);
        changed |= SetIfChanged(edition.IsFree, definition.IsFree, value => edition.IsFree = value);
        changed |= SetIfChanged(edition.IsDefault, definition.IsDefault, value => edition.IsDefault = value);
        changed |= SetIfChanged(edition.Status, EnableStatus.Enabled, value => edition.Status = value);
        changed |= SetIfChanged(edition.Sort, definition.Sort, value => edition.Sort = value);
        changed |= SetIfChanged(edition.Remark, "系统初始化租户版本", value => edition.Remark = value);
        return changed;
    }

    private static SysTenantEditionPermission CreateBinding(long editionId, long permissionId, string editionName)
    {
        var binding = new SysTenantEditionPermission
        {
            EditionId = editionId,
            PermissionId = permissionId
        };
        _ = ApplyBindingDefinition(binding, editionId, permissionId, editionName);
        return binding;
    }

    private static bool ApplyBindingDefinition(SysTenantEditionPermission binding, long editionId, long permissionId, string editionName)
    {
        var changed = false;
        changed |= SetIfChanged(binding.TenantId, 0, value => binding.TenantId = value);
        changed |= SetIfChanged(binding.EditionId, editionId, value => binding.EditionId = value);
        changed |= SetIfChanged(binding.PermissionId, permissionId, value => binding.PermissionId = value);
        changed |= SetIfChanged(binding.Status, ValidityStatus.Valid, value => binding.Status = value);
        changed |= SetIfChanged(binding.Remark, $"系统初始化版本权限：{editionName}", value => binding.Remark = value);
        return changed;
    }

    private static IReadOnlyCollection<string> ResolvePermissionCodes(
        EditionSeedDefinition definition,
        IReadOnlyList<SysPermission> permissions)
    {
        if (!definition.GrantAllTenantSafePermissions)
        {
            return definition.PermissionCodes;
        }

        return permissions
            .Select(permission => permission.PermissionCode)
            .Where(code => !SaasPlatformPermissions.PlatformOnlyCodes.Contains(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<EditionSeedDefinition> BuildDefinitions(string defaultEditionCode)
    {
        var freePermissions = BuildFreePermissionCodes();
        var basicPermissions = Combine(
            freePermissions,
            [
                SaasPermissionCodes.Department.Create,
                SaasPermissionCodes.Department.Update,
                SaasPermissionCodes.Department.Status,
                SaasPermissionCodes.User.Create,
                SaasPermissionCodes.User.Update,
                SaasPermissionCodes.User.Status,
                SaasPermissionCodes.UserSecurity.Read,
                SaasPermissionCodes.UserSecurity.ResetPassword,
                SaasPermissionCodes.UserSession.Revoke,
                SaasPermissionCodes.Role.Create,
                SaasPermissionCodes.Role.Update,
                SaasPermissionCodes.Role.Status,
                SaasPermissionCodes.RolePermission.Grant,
                SaasPermissionCodes.RolePermission.Update,
                SaasPermissionCodes.RolePermission.Status,
                SaasPermissionCodes.RolePermission.Revoke,
                SaasPermissionCodes.UserRole.Grant,
                SaasPermissionCodes.UserRole.Update,
                SaasPermissionCodes.UserRole.Status,
                SaasPermissionCodes.UserRole.Revoke,
                SaasPermissionCodes.UserDepartment.Grant,
                SaasPermissionCodes.UserDepartment.Update,
                SaasPermissionCodes.UserDepartment.Status,
                SaasPermissionCodes.UserDepartment.Revoke,
                SaasPermissionCodes.Notification.Create,
                SaasPermissionCodes.Notification.Update,
                SaasPermissionCodes.Notification.Publish,
                SaasPermissionCodes.Notification.Delete,
                SaasPermissionCodes.MessageTemplate.Read
            ]);
        var proPermissions = Combine(
            basicPermissions,
            [
                SaasPermissionCodes.UserSecurity.Lock,
                SaasPermissionCodes.UserSecurity.LoginPolicy,
                SaasPermissionCodes.RoleHierarchy.Read,
                SaasPermissionCodes.RoleHierarchy.Create,
                SaasPermissionCodes.RoleHierarchy.Delete,
                SaasPermissionCodes.RoleDataScope.Read,
                SaasPermissionCodes.RoleDataScope.Grant,
                SaasPermissionCodes.RoleDataScope.Update,
                SaasPermissionCodes.RoleDataScope.Status,
                SaasPermissionCodes.RoleDataScope.Revoke,
                SaasPermissionCodes.UserPermission.Read,
                SaasPermissionCodes.UserPermission.Grant,
                SaasPermissionCodes.UserPermission.Update,
                SaasPermissionCodes.UserPermission.Status,
                SaasPermissionCodes.UserPermission.Revoke,
                SaasPermissionCodes.UserDataScope.Read,
                SaasPermissionCodes.UserDataScope.Grant,
                SaasPermissionCodes.UserDataScope.Update,
                SaasPermissionCodes.UserDataScope.Status,
                SaasPermissionCodes.UserDataScope.Revoke,
                SaasPermissionCodes.FieldLevelSecurity.Read,
                SaasPermissionCodes.FieldLevelSecurity.Create,
                SaasPermissionCodes.FieldLevelSecurity.Update,
                SaasPermissionCodes.FieldLevelSecurity.Status,
                SaasPermissionCodes.FieldLevelSecurity.Delete,
                SaasPermissionCodes.PermissionDelegation.Read,
                SaasPermissionCodes.PermissionDelegation.Create,
                SaasPermissionCodes.PermissionDelegation.Update,
                SaasPermissionCodes.PermissionDelegation.Status,
                SaasPermissionCodes.PermissionDelegation.Revoke,
                SaasPermissionCodes.PermissionRequest.Read,
                SaasPermissionCodes.PermissionRequest.Create,
                SaasPermissionCodes.PermissionRequest.Update,
                SaasPermissionCodes.PermissionRequest.Status,
                SaasPermissionCodes.PermissionRequest.Withdraw,
                SaasPermissionCodes.PermissionCondition.Read,
                SaasPermissionCodes.PermissionCondition.Create,
                SaasPermissionCodes.PermissionCondition.Update,
                SaasPermissionCodes.PermissionCondition.Status,
                SaasPermissionCodes.PermissionCondition.Delete,
                SaasPermissionCodes.ConstraintRule.Read,
                SaasPermissionCodes.ConstraintRule.Create,
                SaasPermissionCodes.ConstraintRule.Update,
                SaasPermissionCodes.ConstraintRule.Status,
                SaasPermissionCodes.ConstraintRule.Delete,
                SaasPermissionCodes.OAuthApp.Read,
                SaasPermissionCodes.ApiLog.Read,
                SaasPermissionCodes.DiffLog.Read,
                SaasPermissionCodes.ExceptionLog.Read,
                SaasPermissionCodes.PermissionChangeLog.Read,
                SaasPermissionCodes.StorageConfig.Read,
                SaasPermissionCodes.StorageConfig.Create,
                SaasPermissionCodes.StorageConfig.Update,
                SaasPermissionCodes.StorageConfig.Status,
                SaasPermissionCodes.StorageConfig.Delete,
                SaasPermissionCodes.MessageTemplate.Create,
                SaasPermissionCodes.MessageTemplate.Update,
                SaasPermissionCodes.MessageTemplate.Status,
                SaasPermissionCodes.MessageTemplate.Delete
            ]);

        return
        [
            new(
                FreeEditionCode,
                "免费版",
                "适用于个人试用和小团队基础协作",
                5,
                1024,
                0,
                1,
                true,
                IsDefault(FreeEditionCode, defaultEditionCode),
                10,
                freePermissions,
                false),
            new(
                "basic",
                "基础版",
                "适用于小型团队的组织、用户和角色管理",
                20,
                10240,
                99,
                1,
                false,
                IsDefault("basic", defaultEditionCode),
                20,
                basicPermissions,
                false),
            new(
                "pro",
                "专业版",
                "适用于中大型团队的高级权限、审计和安全能力",
                100,
                102400,
                299,
                1,
                false,
                IsDefault("pro", defaultEditionCode),
                30,
                proPermissions,
                false),
            new(
                EnterpriseEditionCode,
                "企业版",
                "适用于企业客户的完整能力和不限配额",
                null,
                null,
                null,
                12,
                false,
                IsDefault(EnterpriseEditionCode, defaultEditionCode),
                40,
                [],
                true)
        ];
    }

    private static string[] BuildFreePermissionCodes()
    {
        return
        [
            SaasPermissionCodes.Tenant.Read,
            SaasPermissionCodes.TenantMember.Read,
            SaasPermissionCodes.TenantMember.Update,
            SaasPermissionCodes.TenantMember.Status,
            SaasPermissionCodes.TenantMember.InviteStatus,
            SaasPermissionCodes.TenantMember.Revoke,
            SaasPermissionCodes.Department.Read,
            SaasPermissionCodes.User.Read,
            SaasPermissionCodes.UserSession.Read,
            SaasPermissionCodes.UserStatistics.Read,
            SaasPermissionCodes.UserDepartment.Read,
            SaasPermissionCodes.Role.Read,
            SaasPermissionCodes.RolePermission.Read,
            SaasPermissionCodes.UserRole.Read,
            SaasPermissionCodes.Permission.Read,
            SaasPermissionCodes.Config.Read,
            SaasPermissionCodes.Dict.Read,
            SaasPermissionCodes.File.Read,
            SaasPermissionCodes.Message.Read,
            SaasPermissionCodes.Notification.Read,
            SaasPermissionCodes.AccessLog.Read,
            SaasPermissionCodes.LoginLog.Read,
            SaasPermissionCodes.OperationLog.Read
        ];
    }

    private static string[] Combine(params IReadOnlyCollection<string>[] parts)
    {
        return parts
            .SelectMany(part => part)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool IsDefault(string editionCode, string defaultEditionCode)
    {
        return string.Equals(editionCode, defaultEditionCode, StringComparison.OrdinalIgnoreCase);
    }

    private static bool SetIfChanged<T>(T current, T next, Action<T> setter)
    {
        if (EqualityComparer<T>.Default.Equals(current, next))
        {
            return false;
        }

        setter(next);
        return true;
    }

    private sealed record EditionSeedDefinition(
        string EditionCode,
        string EditionName,
        string Description,
        int? UserLimit,
        long? StorageLimit,
        decimal? Price,
        int? BillingPeriodMonths,
        bool IsFree,
        bool IsDefault,
        int Sort,
        IReadOnlyCollection<string> PermissionCodes,
        bool GrantAllTenantSafePermissions);

    private sealed record EditionSeedResult(
        IReadOnlyDictionary<string, SysTenantEdition> Editions,
        int AddCount,
        int UpdateCount);

    private sealed record BindingSeedResult(int AddCount, int UpdateCount);
}
