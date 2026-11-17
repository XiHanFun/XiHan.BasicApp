// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;

namespace XiHan.BasicApp.Runtime.Schema;

/// <summary>
/// 本节点所知的、没有升级到当前结构版本的租户独立库
/// </summary>
/// <remarks>
/// 单例。启动升级、重试升级记下成败；<see cref="TenantSchemaWatch"/> 启动时把就绪的独立库逐个核对一遍，之后定时复查不可用的库，别的节点重试成功之后本节点随之放行
/// 这里的租户在恢复之前调用一律 503（<see cref="Middleware.TenantSchemaGateMiddleware"/>），健康检查为降级（<see cref="SchemaHealthContributor"/>）
/// </remarks>
public sealed class SchemaHealth
{
    private readonly ConcurrentDictionary<string, SchemaUpgradeFailure> _failures = new(StringComparer.Ordinal);

    /// <summary>
    /// 不可用的租户独立库
    /// </summary>
    public IReadOnlyList<SchemaUpgradeFailure> Failures => [.. _failures.Values.OrderBy(static f => f.Database, StringComparer.Ordinal)];

    /// <summary>
    /// 租户的独立库是否不可用
    /// </summary>
    /// <param name="tenantId">租户 ID</param>
    public bool IsUnavailable(string tenantId) => _failures.ContainsKey(tenantId);

    /// <summary>
    /// 租户的独立库不可用
    /// </summary>
    /// <param name="failure">原因</param>
    public void Fail(SchemaUpgradeFailure failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        _failures[failure.Database] = failure;
    }

    /// <summary>
    /// 租户的独立库已升到当前结构版本
    /// </summary>
    /// <param name="tenantId">租户 ID</param>
    public void Recover(string tenantId) => _failures.TryRemove(tenantId, out _);
}
