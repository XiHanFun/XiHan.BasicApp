// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Contracts.Seeding;
using XiHan.BasicApp.Runtime.Seeding;
using XiHan.BasicApp.Tenancy.Seeds;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Seeds;

/// <summary>
/// 演示访问策略种子
/// </summary>
/// <remarks>
/// 号段 900 + 模块偏移 20 + 6（排在演示角色之后），<c>BasicApp:Seed:EnableDemoData</c> 打开才写
/// 在演示租户 <c>demo</c> 里写两条只记录的策略（非工作时间、境外访问），内容取同名模板，让命中记录页有数据
/// 按编码只首写：写过一次（含已删除的）就不再碰
/// </remarks>
/// <param name="service">策略服务</param>
/// <param name="policies">策略仓储</param>
/// <param name="options">种子配置</param>
public sealed class DemoAccessPolicySeeder(AccessPolicyService service, IRepository<SysAccessPolicy, Guid> policies, SeedOptions options) : ISeeder
{
    private static readonly (string Code, string TemplateCode)[] _demoPolicies =
    [
        ("demo.off-hours", "preset.off-hours"),
        ("demo.overseas", "preset.overseas"),
    ];

    /// <inheritdoc />
    public string Name => "access.demo-policies";

    /// <inheritdoc />
    public int Order => SeedOrders.Demo + (SeedOrders.ModuleOffset * 2) + 6;

    /// <inheritdoc />
    public async ValueTask<Result> SeedAsync(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!options.EnableDemoData)
        {
            return Result.Success();
        }

        const string tenant = DemoTenantSeeder.DemoTenantId;
        foreach (var (code, templateCode) in _demoPolicies)
        {
            var written = EntityQuery.All<SysAccessPolicy>().Where(new Specification<SysAccessPolicy>(p => p.OwnerTenantId == tenant && p.Code == code)) with { IncludeDeleted = true };
            if (await policies.CountAsync(written, context.CancellationToken).ConfigureAwait(false) > 0)
            {
                continue;
            }

            var template = AccessPolicyTemplates.Find(templateCode) ?? throw new InvalidOperationException($"The access policy template '{templateCode}' does not exist.");
            var created = await service.CreateAsync(tenant, new AccessPolicyDefinition(code, template.Name, template.Description, template.Priority, null, null, template.Code, "演示：只记录，命中记录页可见", template.Conditions), context.CancellationToken).ConfigureAwait(false);
            if (created.IsFailure)
            {
                return created.Error!;
            }
        }

        return Result.Success();
    }
}
