// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders;

/// <summary>
/// SaaS 操作字典：各模块资源型权限共用的动作（<see cref="OperationSeeds"/>）
/// </summary>
/// <remarks>
/// 字典由代码定义：已有的操作对齐名称、类型、分类、HTTP 方法、审计与危险标记、排序，缺的插入；启停归运营，只在插入时启用。
/// </remarks>
public sealed class SaasOperationSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<SaasOperationSeeder> logger,
    IServiceProvider serviceProvider)
    : PlatformDataSeederBase(clientResolver, logger, serviceProvider)
{
    private const string SeededRemark = "系统初始化内置操作";

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => SeedOrders.Operations;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[SaaS]操作字典";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var codes = OperationSeeds.All.Select(static operation => operation.Code).ToList();
        var existing = (await DbClient.Queryable<SysOperation>()
                .Where(operation => operation.TenantId == 0 && codes.Contains(operation.OperationCode))
                .ToListAsync())
            .ToDictionary(operation => operation.OperationCode, StringComparer.Ordinal);

        var adding = new List<SysOperation>();
        var updated = 0;
        foreach (var seed in OperationSeeds.All)
        {
            if (existing.TryGetValue(seed.Code, out var operation))
            {
                if (Apply(operation, seed))
                {
                    _ = await DbClient.Updateable(operation).ExecuteCommandAsync();
                    updated++;
                }

                continue;
            }

            operation = new SysOperation { OperationCode = seed.Code, Status = EnableStatus.Enabled };
            _ = Apply(operation, seed);
            adding.Add(operation);
        }

        if (adding.Count > 0)
        {
            await BulkInsertAsync(adding);
        }

        Logger.LogInformation("{Seeder}：新增 {AddCount} 个、对齐 {UpdateCount} 个", Name, adding.Count, updated);
    }

    private static bool Apply(SysOperation operation, OperationSeed seed)
    {
        var changed = false;
        changed |= SeedValues.SetIfChanged(operation.OperationName, seed.Name, value => operation.OperationName = value);
        changed |= SeedValues.SetIfChanged(operation.OperationTypeCode, seed.Type, value => operation.OperationTypeCode = value);
        changed |= SeedValues.SetIfChanged(operation.Category, seed.Category, value => operation.Category = value);
        changed |= SeedValues.SetIfChanged(operation.HttpMethod, seed.HttpMethod, value => operation.HttpMethod = value);
        changed |= SeedValues.SetIfChanged(operation.IsRequireAudit, seed.IsRequireAudit, value => operation.IsRequireAudit = value);
        changed |= SeedValues.SetIfChanged(operation.IsDangerous, seed.IsDangerous, value => operation.IsDangerous = value);
        changed |= SeedValues.SetIfChanged(operation.Description, $"通用{seed.Name}操作", value => operation.Description = value);
        changed |= SeedValues.SetIfChanged(operation.Sort, seed.Sort, value => operation.Sort = value);
        changed |= SeedValues.SetIfChanged(operation.Remark, SeededRemark, value => operation.Remark = value);
        return changed;
    }
}
