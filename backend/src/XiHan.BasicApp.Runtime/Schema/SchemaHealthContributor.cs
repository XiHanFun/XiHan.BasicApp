// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Runtime.Schema;

/// <summary>
/// 结构升级的健康项
/// </summary>
/// <remarks>
/// 有租户独立库没有升级到当前结构版本即降级（这些租户暂不可用），数据带不可用的库数与库名；平台库升级失败时节点根本起不来，不在这里报
/// </remarks>
/// <param name="health">不可用的租户独立库</param>
public sealed class SchemaHealthContributor(SchemaHealth health) : IHealthContributor
{
    /// <summary>
    /// 健康项名
    /// </summary>
    public const string Name = "basicapp.schema";

    /// <inheritdoc />
    public IReadOnlyList<ContributorStatus> Check()
    {
        var failures = health.Failures;
        var data = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["unavailableTenants"] = failures.Count,
            ["tenants"] = failures.Count == 0 ? null : string.Join(",", failures.Select(static f => f.Database)),
        };
        return failures.Count == 0
            ? [new ContributorStatus(Name, LifecycleState.Ready) { Data = data }]
            : [new ContributorStatus(Name, LifecycleState.Degraded)
            {
                Description = $"{failures.Count} tenant database(s) not upgraded to the current schema version; those tenants are unavailable.",
                Error = Error.Unavailable(RuntimeErrors.TenantSchemaUnavailable, RuntimeMessages.TenantSchemaUnavailable, MessageArguments.Of(("tenant", failures[0].Database))),
                Data = data,
            }];
    }
}
