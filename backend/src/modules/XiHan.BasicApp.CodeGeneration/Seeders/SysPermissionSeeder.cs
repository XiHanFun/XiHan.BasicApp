#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysPermissionSeeder
// Guid:bf16e480-f0f6-49be-b730-c787f4167417
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/12 13:12:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.CodeGeneration.Seeders;

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
    public override int Order => 22;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[CodeGeneration]系统权限种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var client = DbContext.GetClient();
        var resources = await client.Queryable<SysResource>().Where(r => r.ResourceCode == "code_gen" || r.ResourceCode == "code_gen_api").ToListAsync();
        var operations = await client.Queryable<SysOperation>().ToListAsync();
        if (resources.Count == 0 || operations.Count == 0)
        {
            Logger.LogWarning("系统资源或操作不存在，跳过系统权限种子数据"); return;
        }

        var operationMap = operations.ToDictionary(o => o.OperationCode, o => o);
        var target = new Dictionary<string, string[]> { ["code_gen"] = ["read", "create", "update", "delete", "export", "import"], ["code_gen_api"] = ["read", "create", "update", "delete", "execute"] };
        var permissionCodes = target.SelectMany(kv => kv.Value.Select(op => $"{kv.Key}:{op}")).ToList();
        var existing = await client.Queryable<SysPermission>().Where(p => permissionCodes.Contains(p.PermissionCode)).ToListAsync();
        var existingCodes = existing.Select(x => x.PermissionCode).ToHashSet();
        var addList = new List<SysPermission>();

        foreach (var resource in resources)
        {
            if (!target.TryGetValue(resource.ResourceCode, out var opCodes))
            {
                continue;
            }

            foreach (var opCode in opCodes)
            {
                var permissionCode = $"{resource.ResourceCode}:{opCode}";
                if (existingCodes.Contains(permissionCode) || !operationMap.TryGetValue(opCode, out var operation))
                {
                    continue;
                }

                addList.Add(new SysPermission { ResourceId = resource.BasicId, OperationId = operation.BasicId, PermissionCode = permissionCode, PermissionName = $"{resource.ResourceName}-{operation.OperationName}", PermissionDescription = $"对{resource.ResourceName}执行{operation.OperationName}操作", IsRequireAudit = operation.IsRequireAudit, Tags = "codegen", Status = YesOrNo.Yes, Sort = 900 + addList.Count });
            }
        }

        if (addList.Count == 0)
        {
            Logger.LogInformation("系统权限数据已存在，跳过种子数据"); return;
        }
        await BulkInsertAsync(addList);
        Logger.LogInformation("成功初始化 {Count} 个系统权限", addList.Count);
    }
}
