// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.AI.Infrastructure.Seeders.System;

/// <summary>
/// 系统操作种子数据
/// </summary>
/// <remarks>
/// 播种 AI 模块所需的通用操作字典（read/create/update/delete/execute），作为权限点的"动作"维度。
/// 必须先于 <see cref="SysPermissionSeeder"/> 执行：后者按「资源 × 操作」派生 ai:* 权限，
/// 若本表为空则整条 AI 权限/菜单/授权链在干净库上静默跳过。
/// 操作字典为全局共享（TenantId=0），与代码生成模块的同名操作幂等共存（按编码去重）。
/// </remarks>
public class SysOperationSeeder : DataSeederBase
{
    /// <summary>
    /// 内置通用操作定义（操作编码与 <see cref="SysPermissionSeeder"/> 的目标操作集一致）
    /// </summary>
    private static readonly (string Code, string Name, OperationTypeCode Type, OperationCategory Category, HttpMethodType Http, bool Audit, bool Dangerous, int Sort)[] BuiltInOperations =
    [
        ("read", "读取", OperationTypeCode.Read, OperationCategory.Crud, HttpMethodType.GET, false, false, 1),
        ("create", "创建", OperationTypeCode.Create, OperationCategory.Crud, HttpMethodType.POST, true, false, 2),
        ("update", "更新", OperationTypeCode.Update, OperationCategory.Crud, HttpMethodType.PUT, true, false, 3),
        ("delete", "删除", OperationTypeCode.Delete, OperationCategory.Crud, HttpMethodType.DELETE, true, true, 4),
        ("execute", "执行", OperationTypeCode.Execute, OperationCategory.Business, HttpMethodType.POST, true, false, 7)
    ];

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysOperationSeeder(ISqlSugarClientResolver clientResolver, ILogger<SysOperationSeeder> logger, IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 种子数据优先级（AI 种子统一在 Order 200+ 独立段；操作字典须先于 SysPermissionSeeder）
    /// </summary>
    public override int Order => 200;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Ai]系统操作种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var client = DbClient;
        var codes = BuiltInOperations.Select(o => o.Code).ToList();
        var existingCodes = (await client.Queryable<SysOperation>()
                .Where(o => codes.Contains(o.OperationCode))
                .ToListAsync())
            .Select(o => o.OperationCode)
            .ToHashSet();

        var addList = BuiltInOperations
            .Where(o => !existingCodes.Contains(o.Code))
            .Select(o => new SysOperation
            {
                TenantId = 0,
                OperationCode = o.Code,
                OperationName = o.Name,
                OperationTypeCode = o.Type,
                Category = o.Category,
                HttpMethod = o.Http,
                IsRequireAudit = o.Audit,
                IsDangerous = o.Dangerous,
                Status = EnableStatus.Enabled,
                Sort = o.Sort,
                Description = $"通用{o.Name}操作",
                Remark = "系统内置操作（权限点动作维度）"
            })
            .ToList();

        if (addList.Count == 0)
        {
            Logger.LogInformation("系统操作数据已存在，跳过种子数据");
            return;
        }

        await BulkInsertAsync(addList);
        Logger.LogInformation("成功初始化 {Count} 个系统操作", addList.Count);
    }
}
