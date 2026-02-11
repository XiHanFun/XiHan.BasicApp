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
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Rbac.Seeders;

/// <summary>
/// 系统权限种子数据
/// </summary>
public class SysPermissionSeeder : DataSeederBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public SysPermissionSeeder(ISqlSugarDbContext dbContext, ILogger<SysPermissionSeeder> logger, IServiceProvider serviceProvider)
        : base(dbContext, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 2;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "系统权限种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        if (await HasDataAsync<SysPermission>(p => true))
        {
            Logger.LogInformation("系统权限数据已存在，跳过种子数据");
            return;
        }

        // 获取资源和操作
        var resources = await DbContext.GetClient().Queryable<SysResource>().ToListAsync();
        var operations = await DbContext.GetClient().Queryable<SysOperation>().ToListAsync();

        if (resources.Count == 0 || operations.Count == 0)
        {
            Logger.LogWarning("资源或操作数据不存在，跳过权限种子数据");
            return;
        }

        var permissions = new List<SysPermission>();
        var permissionId = 1;

        // 为每个资源创建权限（除了系统管理根节点）
        var resourcesNeedPermissions = resources.Where(r => r.ResourceCode != "system").ToList();

        foreach (var resource in resourcesNeedPermissions)
        {
            // 根据资源类型决定需要哪些操作
            var operationCodes = GetOperationsForResource(resource.ResourceType);

            foreach (var opCode in operationCodes)
            {
                var operation = operations.FirstOrDefault(o => o.OperationCode == opCode);
                if (operation != null)
                {
                    permissions.Add(new SysPermission
                    {
                        ResourceId = resource.BasicId,
                        OperationId = operation.BasicId,
                        PermissionCode = $"{resource.ResourceCode}:{operation.OperationCode}",
                        PermissionName = $"{resource.ResourceName}-{operation.OperationName}",
                        PermissionDescription = $"对{resource.ResourceName}执行{operation.OperationName}操作",
                        IsRequireAudit = operation.IsRequireAudit,
                        Tags = GetPermissionTags(resource.ResourceCode, operation.OperationCode),
                        Status = YesOrNo.Yes,
                        Sort = permissionId
                    });
                    permissionId++;
                }
            }
        }

        await BulkInsertAsync(permissions);
        Logger.LogInformation($"成功初始化 {permissions.Count} 个系统权限");
    }

    /// <summary>
    /// 根据资源类型获取需要的操作
    /// </summary>
    private static List<string> GetOperationsForResource(ResourceType resourceType)
    {
        return resourceType switch
        {
            ResourceType.Menu => ["read", "create", "update", "delete", "export", "import"],
            ResourceType.Api => ["read", "create", "update", "delete", "execute"],
            ResourceType.Button => ["view", "execute"],
            _ => ["read", "create", "update", "delete"]
        };
    }

    /// <summary>
    /// 获取权限标签
    /// </summary>
    private static string GetPermissionTags(string resourceCode, string operationCode)
    {
        var tags = new List<string>();

        // 敏感操作标签
        if (operationCode is "delete" or "revoke" or "disable")
        {
            tags.Add("sensitive");
        }

        // 管理员操作标签
        if (resourceCode is "role" or "permission" or "tenant")
        {
            tags.Add("admin");
        }

        // 审计相关标签
        if (resourceCode.Contains("log") || resourceCode.Contains("audit"))
        {
            tags.Add("audit");
        }

        return tags.Count != 0 ? string.Join(",", tags) : "";
    }
}
