// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.MultiTenancy;
using XiHan.BasicApp.Tenancy.Domain.Entities;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Tenancy.Domain.Services;

/// <summary>
/// 租户的自定义域名
/// </summary>
/// <remarks>
/// 存小写、不带端口与末尾的点；只用于登录页按域名显示品牌与预填租户，身份仍只认令牌
/// </remarks>
/// <param name="tenants">租户仓储</param>
/// <param name="clock">时间源</param>
public sealed class TenantDomains(IRepository<SysTenant, Guid> tenants, TimeProvider clock) : ITenantDomainResolver
{
    /// <summary>
    /// 规范化：去空白、小写、去端口与末尾的点
    /// </summary>
    /// <param name="host">主机名，可以带端口（IPv6 写在方括号里）</param>
    /// <returns>没有内容为 <see langword="null"/></returns>
    public static string? Normalize(string? host)
    {
        var text = host?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        if (text.StartsWith('['))
        {
            var close = text.IndexOf(']', StringComparison.Ordinal);
            text = close < 0 ? text : text[..(close + 1)];
        }
        else if (text.IndexOf(':', StringComparison.Ordinal) is var colon and >= 0 && text.IndexOf(':', colon + 1) < 0)
        {
            text = text[..colon];
        }

        text = text.TrimEnd('.');
        return text.Length == 0 ? null : text;
    }

    /// <summary>
    /// 已被别的租户绑定
    /// </summary>
    /// <param name="domain">规范化后的域名</param>
    /// <param name="except">正在修改的租户（主键）；新建为 <see langword="null"/></param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<bool> IsTakenAsync(string domain, Guid? except, CancellationToken cancellationToken)
    {
        var rows = await tenants.ListAsync(EntityQuery.All<SysTenant>().Where(new Specification<SysTenant>(t => t.Domain == domain)).Page(0, 2), cancellationToken).ConfigureAwait(false);
        return rows.Any(t => t.Id != except);
    }

    /// <inheritdoc />
    public async Task<Maybe<string>> FindTenantAsync(string host, CancellationToken cancellationToken)
    {
        if (Normalize(host) is not { } domain)
        {
            return Maybe.None<string>();
        }

        var rows = await tenants.ListAsync(EntityQuery.All<SysTenant>().Where(new Specification<SysTenant>(t => t.Domain == domain)).Page(0, 1), cancellationToken).ConfigureAwait(false);
        return rows.Count > 0 && rows[0] is { Status: TenantStatus.Active } tenant && (tenant.ExpiresAt is not { } expires || expires > clock.GetUtcNow())
            ? Maybe.Some(tenant.TenantId)
            : Maybe.None<string>();
    }
}
