// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Frozen;
using Microsoft.Extensions.Primitives;

namespace XiHan.BasicApp.Contracts.Security;

/// <summary>
/// 已登记的全部权限描述（单例）
/// </summary>
/// <remarks>
/// 各包启动期登记的 <see cref="PermissionCatalogSection"/> 与运行期来源（<see cref="IRuntimePermissionSource"/>）当前的片段合在一起，来源触发变更令牌即重建
/// Access 的权限目录、审计与套餐都从这里取，运行期贡献的权限码因此带着作用侧与审计标记进授权、审计与套餐
/// </remarks>
public sealed class RegisteredPermissions : IDisposable
{
    private readonly Lock _gate = new();
    private readonly IRuntimePermissionSource[] _sources;
    private readonly List<IDisposable> _subscriptions = [];
    private Snapshot _current = Snapshot.Empty;

    /// <summary>
    /// 汇总启动期片段并订阅运行期来源
    /// </summary>
    /// <param name="sections">各包登记的权限目录片段</param>
    /// <param name="sources">运行期来源</param>
    public RegisteredPermissions(IEnumerable<PermissionCatalogSection> sections, IEnumerable<IRuntimePermissionSource> sources)
    {
        ArgumentNullException.ThrowIfNull(sections);
        ArgumentNullException.ThrowIfNull(sources);
        BuiltIn = Sorted(sections.SelectMany(static s => s.Permissions));
        _sources = [.. sources];

        // 先订阅再汇总：订阅与汇总之间的变动不会漏掉
        foreach (var source in _sources)
        {
            _subscriptions.Add(ChangeToken.OnChange(source.GetChangeToken, Rebuild));
        }

        Rebuild();
    }

    /// <summary>
    /// 各包启动期登记的权限描述，按权限码排序
    /// </summary>
    public IReadOnlyList<PermissionDescriptor> BuiltIn { get; }

    /// <summary>
    /// 当前的全部权限描述，按权限码排序
    /// </summary>
    public IReadOnlyList<PermissionDescriptor> All => Volatile.Read(ref _current).All;

    /// <summary>
    /// 按权限码取描述
    /// </summary>
    /// <param name="code">权限码</param>
    /// <returns>描述；没有登记时为空</returns>
    public PermissionDescriptor? Find(string code)
    {
        ArgumentNullException.ThrowIfNull(code);
        return Volatile.Read(ref _current).ByCode.GetValueOrDefault(code);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }

        _subscriptions.Clear();
    }

    /// <summary>
    /// 重新汇总
    /// </summary>
    /// <remarks>
    /// 权限码重复即抛出：运行期来源不该给出已登记的码，抛给触发变动的一方
    /// </remarks>
    private void Rebuild()
    {
        lock (_gate)
        {
            var all = Sorted(BuiltIn.Concat(_sources.SelectMany(static s => s.Sections).SelectMany(static s => s.Permissions)));
            Volatile.Write(ref _current, new Snapshot(all, all.ToFrozenDictionary(static p => p.Code, StringComparer.Ordinal)));
        }
    }

    private static List<PermissionDescriptor> Sorted(IEnumerable<PermissionDescriptor> permissions) => [.. permissions.OrderBy(static p => p.Code, StringComparer.Ordinal)];

    /// <summary>
    /// 一次汇总的结果
    /// </summary>
    /// <param name="All">全部描述</param>
    /// <param name="ByCode">按权限码索引</param>
    private sealed record Snapshot(IReadOnlyList<PermissionDescriptor> All, FrozenDictionary<string, PermissionDescriptor> ByCode)
    {
        public static Snapshot Empty { get; } = new([], FrozenDictionary<string, PermissionDescriptor>.Empty);
    }
}
