// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;
using XiHan.Framework.Kernel.Operations;

namespace XiHan.BasicApp.Auditing.Domain.Services;

/// <summary>
/// 哪些操作进操作日志：权限描述标了「审计」（D4 的 <see cref="PermissionDescriptor.Audited"/>）的权限所保护的操作。
/// 框架审计中间件的选择规则设为 <c>None</c>，由本包的中间件对这些操作显式声明审计动作，读操作因此不进操作日志（它们在访问日志里）。
/// </summary>
/// <param name="sections">各包登记的权限描述。</param>
public sealed class AuditedOperations(IEnumerable<PermissionCatalogSection> sections)
{
    private readonly HashSet<string> _audited = sections.SelectMany(static s => s.Permissions).Where(static p => p.Audited).Select(static p => p.Code).ToHashSet(StringComparer.Ordinal);

    /// <summary>
    /// 操作是否进操作日志。
    /// </summary>
    public bool IsAudited(OperationDescriptor operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return operation.Contract.Permission is { } permission && _audited.Contains(permission);
    }
}
