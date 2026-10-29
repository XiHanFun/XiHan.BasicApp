// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Kernel.Pipeline;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Contracts.Auditing;

/// <summary>
/// 在调用上下文里登记业务日志。
/// </summary>
/// <remarks>
/// Tier 1 的包（Identity、Tenancy、Access）不引用 Auditing，只在这里登记；
/// Auditing 的 Observe 阶段中间件在调用结束后取走并落库（派生上下文共享同一个对象袋，Handle 阶段登记的在 Observe 阶段看得见）。
/// </remarks>
public static class AuditLogInvocationExtensions
{
    private static readonly ContextItemKey<List<AuditLogEntry>> _entriesKey = new("basicapp.audit.entries");

    /// <summary>
    /// 登记一条日志。
    /// </summary>
    /// <remarks>
    /// 同一次调用可以登记多条（批量操作逐个目标一条）。
    /// </remarks>
    public static void AddAuditLog(this InvocationContext context, AuditLogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(entry);
        if (!context.Get(_entriesKey).TryGetValue(out var entries))
        {
            entries = [];
            context.Set(_entriesKey, entries);
        }

        entries.Add(entry);
    }

    /// <summary>
    /// 取本次调用登记的日志。
    /// </summary>
    /// <returns>按登记顺序排列的日志；没有为空。</returns>
    public static IReadOnlyList<AuditLogEntry> GetAuditLogs(this InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Get(_entriesKey).TryGetValue(out var entries) ? entries : [];
    }
}
