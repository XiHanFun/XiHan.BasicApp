// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 当前租户替身：可嵌套切换，Dispose 还原上一层，语义与框架 CurrentTenant 一致。
/// </summary>
/// <param name="initialTenantId">初始租户上下文（null 为平台态）。</param>
public sealed class TestCurrentTenant(long? initialTenantId = null) : ICurrentTenant
{
    /// <summary>
    /// 当前是否处于租户上下文。
    /// </summary>
    public bool IsAvailable => Id.HasValue;

    /// <summary>
    /// 当前租户主键，null 为平台态。
    /// </summary>
    public long? Id { get; private set; } = initialTenantId;

    /// <summary>
    /// 当前租户名称。
    /// </summary>
    public string? Name { get; private set; }

    /// <summary>
    /// 切换当前租户上下文。
    /// </summary>
    /// <param name="id">目标租户主键，null 表示平台态。</param>
    /// <param name="name">目标租户名称。</param>
    /// <returns>作用域句柄，Dispose 时还原上一层。</returns>
    public IDisposable Change(long? id, string? name = null)
    {
        var previousId = Id;
        var previousName = Name;
        Id = id;
        Name = name;
        return new RestoreScope(this, previousId, previousName);
    }

    /// <summary>
    /// 还原上一层租户上下文的作用域句柄。
    /// </summary>
    private sealed class RestoreScope(TestCurrentTenant owner, long? previousId, string? previousName) : IDisposable
    {
        private bool _disposed;

        /// <summary>
        /// 还原上一层租户上下文。
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            owner.Id = previousId;
            owner.Name = previousName;
        }
    }
}
