#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysPermissionSeeder
// Guid:3c4d5e6f-7890-1234-cdef-223344556677
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/07 12:20:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Saas.Seeders;

/// <summary>
/// 系统权限种子数据。
/// </summary>
public class SysPermissionSeeder : DataSeederBase
{
    public SysPermissionSeeder(
        ISqlSugarClientResolver clientResolver,
        ILogger<SysPermissionSeeder> logger,
        IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
    {
    }

    public override int Order => SaasSeedOrder.Permissions;

    public override string Name => "[Saas]系统权限种子数据";

    protected override async Task SeedInternalAsync()
    {
        var resources = await DbClient
            .Queryable<SysResource>()
            .Where(resource => resource.TenantId == SaasSeedDefaults.PlatformTenantId && resource.IsGlobal)
            .ToListAsync();
        var operations = await DbClient
            .Queryable<SysOperation>()
            .Where(operation => operation.TenantId == SaasSeedDefaults.PlatformTenantId && operation.IsGlobal)
            .ToListAsync();

        if (resources.Count == 0 || operations.Count == 0)
        {
            Logger.LogWarning("资源或操作模板不存在，跳过权限模板种子");
            return;
        }

        var operationMap = operations.ToDictionary(operation => operation.OperationCode, StringComparer.OrdinalIgnoreCase);
        var templates = new List<SysPermission>();
        var sort = 1000;

        foreach (var resource in resources.OrderBy(item => item.Sort).ThenBy(item => item.ResourceCode, StringComparer.OrdinalIgnoreCase))
        {
            foreach (var operationCode in ResolveOperationCodes(resource.ResourceCode, resource.ResourceType))
            {
                if (!operationMap.TryGetValue(operationCode, out var operation))
                {
                    continue;
                }

                templates.Add(new SysPermission
                {
                    TenantId = SaasSeedDefaults.PlatformTenantId,
                    IsGlobal = true,
                    ResourceId = resource.BasicId,
                    OperationId = operation.BasicId,
                    PermissionCode = $"{resource.ResourceCode}:{operation.OperationCode}",
                    PermissionName = $"{resource.ResourceName}-{operation.OperationName}",
                    PermissionDescription = $"{resource.ResourceName}的{operation.OperationName}权限",
                    Tags = ResolvePermissionTags(resource.ResourceCode, operation.OperationCode),
                    IsRequireAudit = operation.IsRequireAudit,
                    Priority = ResolvePermissionPriority(resource.ResourceCode, operation.OperationCode),
                    Status = YesOrNo.Yes,
                    Sort = sort
                });

                sort += 10;
            }
        }

        var permissionCodes = templates.Select(item => item.PermissionCode).ToArray();
        var existingPermissions = await DbClient
            .Queryable<SysPermission>()
            .Where(permission => permissionCodes.Contains(permission.PermissionCode))
            .ToListAsync();

        var existingMap = existingPermissions.ToDictionary(permission => permission.PermissionCode, StringComparer.OrdinalIgnoreCase);
        var toInsert = templates
            .Where(template => !existingMap.ContainsKey(template.PermissionCode))
            .ToList();

        if (toInsert.Count > 0)
        {
            await BulkInsertAsync(toInsert);
        }

        var toEnableIds = existingPermissions
            .Where(permission =>
                permission.TenantId == SaasSeedDefaults.PlatformTenantId
                && permission.IsGlobal
                && permission.Status != YesOrNo.Yes)
            .Select(permission => permission.BasicId)
            .ToArray();

        if (toEnableIds.Length > 0)
        {
            await DbClient
                .Updateable<SysPermission>()
                .SetColumns(permission => permission.Status == YesOrNo.Yes)
                .Where(permission => toEnableIds.Contains(permission.BasicId))
                .ExecuteCommandAsync();
        }

        Logger.LogInformation(
            "系统权限模板种子完成：新增 {InsertCount} 项，启用 {EnableCount} 项",
            toInsert.Count,
            toEnableIds.Length);
    }

    private static IReadOnlyList<string> ResolveOperationCodes(string resourceCode, ResourceType resourceType)
    {
        if (resourceCode.EndsWith("_log", StringComparison.OrdinalIgnoreCase))
        {
            return ["read", "view", "export"];
        }

        if (resourceCode is "constraint_rule" or "field_level_security" or "role" or "permission" or "tenant")
        {
            return ["read", "view", "create", "update", "delete", "grant", "revoke", "enable", "disable", "export"];
        }

        if (resourceCode is "user" or "department" or "config" or "dict" or "notification" or "oauth_app" or "user_session" or "review" or "task")
        {
            return ["read", "view", "create", "update", "delete", "enable", "disable", "export"];
        }

        if (resourceCode is "message" or "email" or "sms")
        {
            return ["read", "view", "create", "update", "delete", "execute", "export"];
        }

        if (resourceCode is "cache" or "monitor")
        {
            return ["read", "view", "execute"];
        }

        return resourceType switch
        {
            ResourceType.File => ["read", "view", "create", "update", "delete", "upload", "download"],
            _ => ["read", "view", "create", "update", "delete", "export"]
        };
    }

    private static string? ResolvePermissionTags(string resourceCode, string operationCode)
    {
        var tags = new List<string>();

        if (resourceCode is "tenant" or "permission" or "role" or "constraint_rule" or "field_level_security" or "menu")
        {
            tags.Add("platform");
        }

        if (resourceCode.EndsWith("_log", StringComparison.OrdinalIgnoreCase))
        {
            tags.Add("audit");
        }

        if (operationCode is "delete" or "disable" or "revoke" or "grant")
        {
            tags.Add("sensitive");
        }

        return tags.Count == 0 ? null : string.Join(",", tags.Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static int ResolvePermissionPriority(string resourceCode, string operationCode)
    {
        if (resourceCode is "tenant" or "permission" or "constraint_rule" or "field_level_security")
        {
            return 100;
        }

        if (operationCode is "delete" or "revoke" or "disable")
        {
            return 80;
        }

        if (operationCode is "grant" or "enable" or "execute")
        {
            return 60;
        }

        return 10;
    }
}
