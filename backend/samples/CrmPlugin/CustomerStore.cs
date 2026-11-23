// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;

namespace CrmPlugin;

/// <summary>
/// 按租户分开的客户（进程内）
/// </summary>
/// <remarks>
/// 租户取宿主随调用交来的 Baggage <c>xihan.tenant</c>，平台侧没有这个键即空串；进程重启数据即丢，示例只为演示租户隔离
/// </remarks>
internal sealed class CustomerStore
{
    /// <summary>
    /// 租户 → 客户 ID → 客户
    /// </summary>
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, Customer>> _byTenant = new(StringComparer.Ordinal);

    /// <summary>
    /// 一个租户的全部客户，按名称
    /// </summary>
    /// <param name="tenant">租户</param>
    public IReadOnlyList<Customer> List(string tenant)
        => _byTenant.TryGetValue(tenant, out var customers) ? [.. customers.Values.OrderBy(static c => c.Name, StringComparer.Ordinal)] : [];

    /// <summary>
    /// 新建客户
    /// </summary>
    /// <param name="tenant">租户</param>
    /// <param name="name">名称</param>
    /// <param name="email">邮箱，可空</param>
    public Customer Create(string tenant, string name, string? email)
    {
        var customer = new Customer(Guid.NewGuid(), name, email);
        _byTenant.GetOrAdd(tenant, static _ => new ConcurrentDictionary<Guid, Customer>())[customer.Id] = customer;
        return customer;
    }

    /// <summary>
    /// 修改客户的名称与邮箱
    /// </summary>
    /// <param name="tenant">租户</param>
    /// <param name="id">客户 ID</param>
    /// <param name="name">名称</param>
    /// <param name="email">邮箱，可空</param>
    /// <returns>改后的客户；这个租户下没有它时为空</returns>
    public Customer? Update(string tenant, Guid id, string name, string? email)
    {
        if (!_byTenant.TryGetValue(tenant, out var customers) || !customers.ContainsKey(id))
        {
            return null;
        }

        var customer = new Customer(id, name, email);
        customers[id] = customer;
        return customer;
    }

    /// <summary>
    /// 删除客户
    /// </summary>
    /// <param name="tenant">租户</param>
    /// <param name="id">客户 ID</param>
    /// <returns>这个租户下没有它时为假</returns>
    public bool Delete(string tenant, Guid id)
        => _byTenant.TryGetValue(tenant, out var customers) && customers.TryRemove(id, out _);
}
