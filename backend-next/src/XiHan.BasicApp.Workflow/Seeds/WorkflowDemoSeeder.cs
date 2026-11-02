// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts.MultiTenancy;
using XiHan.BasicApp.Contracts.Seeding;
using XiHan.BasicApp.Contracts.Users;
using XiHan.BasicApp.Runtime.Seeding;
using XiHan.BasicApp.Workflow.Domain.Services;
using XiHan.Framework.Primitives;
using XiHan.Framework.Workflow.Abstractions;
using XiHan.Framework.Workflow.Abstractions.Definitions;
using XiHan.Framework.Workflow.Abstractions.Serialization;

namespace XiHan.BasicApp.Workflow.Seeds;

/// <summary>
/// 演示流程种子。
/// </summary>
/// <remarks>
/// 号段 900 + 模块偏移 30（照 4.x 的 Demo + 30），<c>BasicApp:Seed:EnableDemoData</c> 打开才写。在演示租户 <c>demo</c> 里建并发布 4.x 的四个演示流程：
/// 费用报销（直属主管或签，超过 10000 再由总经理审批）、请假（两级主管依次审批，找不到人转管理员，抄送人事，超过 3 天再由人事部审批）、
/// 采购（指定用户会签与总部负责人审核并行，汇聚后总经理审批，120 秒没办走超时）、入职（设置变量、人事准备抄送发起人、导师确认、等 60 秒、
/// 逐件起采购子流程、差旅报销子流程）。定义是嵌入的 WorkflowJson，<c>$user:登录名$</c> 换成演示成员的用户 ID（缺了谁就不建那个流程）；
/// 按名字只首写（这个名字已有任何版本即跳过）。流程数据在租户分区，经 <see cref="ITenantScopeRunner"/> 以 <c>demo</c> 另起一次调用写入。
/// </remarks>
/// <param name="runner">按租户发起调用的运行器。</param>
/// <param name="options">种子配置。</param>
public sealed class WorkflowDemoSeeder(ITenantScopeRunner runner, SeedOptions options) : ISeeder
{
    /// <summary>
    /// 演示租户（Tenancy 的演示租户种子写入）。
    /// </summary>
    public const string DemoTenantId = "demo";

    /// <summary>
    /// 四个演示流程的名字（子流程在前，发起时按名字取已发布的版本）。
    /// </summary>
    public static IReadOnlyList<string> Names { get; } = ["demo-expense", "demo-leave", "demo-procurement", "demo-onboarding"];

    /// <inheritdoc />
    public string Name => "workflow.demo";

    /// <inheritdoc />
    public int Order => SeedOrders.Demo + (SeedOrders.ModuleOffset * 3);

    /// <inheritdoc />
    public async ValueTask<Result> SeedAsync(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!options.EnableDemoData)
        {
            return Result.Success();
        }

        return await runner.RunAsync(DemoTenantId, SeedTenantAsync, context.CancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 读一个嵌入的演示定义。
    /// </summary>
    /// <param name="name">流程名。</param>
    /// <returns>WorkflowJson 文本（用户占位符未替换）。</returns>
    public static string Load(string name)
    {
        using var stream = typeof(WorkflowDemoSeeder).Assembly.GetManifestResourceStream($"Demo/{name}.json")
            ?? throw new InvalidOperationException($"The demo workflow '{name}' is not embedded.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static async ValueTask<Result> SeedTenantAsync(InvocationContext context)
    {
        var catalog = context.Services.GetRequiredService<IWorkflowDefinitionCatalog>();
        var users = await context.Services.GetRequiredService<IUserDirectory>().SearchActiveInSideAsync(DemoTenantId, null, 200, context.CancellationToken).ConfigureAwait(false);
        var ids = users.ToDictionary(static u => u.UserName, static u => WorkflowIds.ToEngineUser(u.Id), StringComparer.Ordinal);
        foreach (var name in Names)
        {
            var existing = await catalog.QueryAsync(new WorkflowDefinitionQuery { Name = new WorkflowName(name), Limit = 1 }, context.CancellationToken).ConfigureAwait(false);
            if (!existing.TryGetValue(out var page))
            {
                return existing.ToResult();
            }

            if (page.Total > 0 || !TryResolve(Load(name), ids, out var document))
            {
                continue;
            }

            var json = (JsonObject)JsonNode.Parse(document)!;
            json["id"] = WorkflowIds.ToEngine(Guid.CreateVersion7());
            json["version"] ??= 1;
            var parsed = WorkflowJson.DeserializeDefinition(json.ToJsonString());
            if (!parsed.TryGetValue(out var definition))
            {
                return parsed.ToResult();
            }

            var created = await catalog.CreateAsync(definition, context.CancellationToken).ConfigureAwait(false);
            if (!created.TryGetValue(out var draft))
            {
                return created.ToResult();
            }

            var published = await catalog.PublishAsync(draft.Id, context.CancellationToken).ConfigureAwait(false);
            if (published.IsFailure)
            {
                return published.ToResult();
            }
        }

        return Result.Success();
    }

    private static bool TryResolve(string document, Dictionary<string, string> ids, out string resolved)
    {
        resolved = document;
        var start = resolved.IndexOf("$user:", StringComparison.Ordinal);
        while (start >= 0)
        {
            var end = resolved.IndexOf('$', start + 6);
            var userName = resolved[(start + 6)..end];
            if (!ids.TryGetValue(userName, out var id))
            {
                return false;
            }

            resolved = string.Concat(resolved.AsSpan(0, start), id, resolved.AsSpan(end + 1));
            start = resolved.IndexOf("$user:", StringComparison.Ordinal);
        }

        return true;
    }
}
