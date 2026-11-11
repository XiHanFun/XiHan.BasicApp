// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions.Permissions;
using XiHan.Framework.Security.Database.Grants;

namespace XiHan.BasicApp.Access.Infrastructure;

/// <summary>
/// 用户的直接授予
/// </summary>
/// <remarks>
/// 读写框架的 <c>PermissionGrantRecord</c>（主体 <c>user:&lt;ID&gt;</c>，租户列按侧——平台侧为全局，租户侧为该租户）
/// 与 Identity 种子写给超管的 <c>*</c> 同一处存放，所以这里列出的也包括别的包直接写入的授予
/// </remarks>
/// <param name="store">框架授予存储</param>
public sealed class UserGrants(PermissionGrantRecordStore store)
{
    /// <summary>
    /// 授予主体
    /// </summary>
    public static GrantSubject SubjectOf(Guid userId) => GrantSubject.ForUser(userId.ToString("D"));

    /// <summary>
    /// 授予写在哪个租户列：平台侧为全局（<see langword="null"/>）
    /// </summary>
    public static string? TenantOf(string owner) => string.Equals(owner, TenantIds.Platform, StringComparison.Ordinal) ? null : owner;

    /// <summary>
    /// 用户在这一侧的直接授予
    /// </summary>
    public async Task<Result<IReadOnlyList<string>>> ListAsync(Guid userId, string owner, CancellationToken cancellationToken)
    {
        // 只要这一侧的授予：平台一侧是全局授予（租户为空）
        var tenant = TenantOf(owner) is { } id ? GrantTenantFilter.Of(id) : GrantTenantFilter.Global;
        var listed = await store.ListAsync(new PermissionGrantQuery { Subject = SubjectOf(userId), Tenant = tenant, Take = PermissionGrantQuery.MaxTake }, cancellationToken).ConfigureAwait(false);
        if (!listed.TryGetValue(out var page))
        {
            return listed.Error!;
        }

        return page.Items.Select(static g => g.Permission).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// 给用户授予权限
    /// </summary>
    /// <returns>逐项结果与入参同序</returns>
    public async ValueTask<IReadOnlyList<PermissionOutcome>> GrantAsync(Guid userId, string owner, IReadOnlyList<string> permissions, CancellationToken cancellationToken)
    {
        var grants = permissions.Select(p => new PermissionGrant(SubjectOf(userId), p, TenantOf(owner))).ToList();
        return PermissionOutcome.Zip(grants, await store.GrantAsync(grants, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// 撤销用户的权限
    /// </summary>
    /// <returns>逐项结果与入参同序（没有的授予是 NotFound）</returns>
    public async ValueTask<IReadOnlyList<PermissionOutcome>> RevokeAsync(Guid userId, string owner, IReadOnlyList<string> permissions, CancellationToken cancellationToken)
    {
        var grants = permissions.Select(p => new PermissionGrant(SubjectOf(userId), p, TenantOf(owner))).ToList();
        return PermissionOutcome.Zip(grants, await store.RevokeAsync(grants, cancellationToken).ConfigureAwait(false));
    }
}
