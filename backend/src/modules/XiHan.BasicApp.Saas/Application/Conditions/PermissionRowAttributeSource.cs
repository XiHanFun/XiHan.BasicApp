// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Authorization.Conditions;

namespace XiHan.BasicApp.Saas.Application.Conditions;

/// <summary>
/// 叠加数据行属性的属性来源：权限码登记了条件资源时，<c>resource.&lt;名&gt;</c> 从数据行取，其余交给请求的来源
/// </summary>
/// <remarks>
/// 鉴权时还没有数据行，行属性一律「暂不可得」，条件集因此可能是待定；
/// 模型绑定之后带上读出的行再算，行不存在或读不到时行属性取不到。
/// </remarks>
public sealed class PermissionRowAttributeSource : IConditionAttributeSource
{
    private readonly IConditionAttributeSource _inner;
    private readonly PermissionResourceRegistration? _registration;
    private readonly bool _rowLoaded;
    private readonly object? _row;

    private PermissionRowAttributeSource(IConditionAttributeSource inner, PermissionResourceRegistration? registration, bool rowLoaded, object? row)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
        _registration = registration;
        _rowLoaded = rowLoaded;
        _row = row;
    }

    /// <summary>
    /// 鉴权时：行属性暂不可得
    /// </summary>
    public static PermissionRowAttributeSource BeforeBinding(IConditionAttributeSource inner, PermissionResourceRegistration? registration)
    {
        return new PermissionRowAttributeSource(inner, registration, rowLoaded: false, row: null);
    }

    /// <summary>
    /// 模型绑定之后：带上读出的行（读不到为空）
    /// </summary>
    public static PermissionRowAttributeSource WithRow(IConditionAttributeSource inner, PermissionResourceRegistration registration, object? row)
    {
        ArgumentNullException.ThrowIfNull(registration);
        return new PermissionRowAttributeSource(inner, registration, rowLoaded: true, row);
    }

    /// <inheritdoc />
    public ValueTask<ConditionAttributeValue> GetAsync(string canonicalName, CancellationToken cancellationToken = default)
    {
        if (_registration is null || !_registration.IsRowAttribute(canonicalName))
        {
            return _inner.GetAsync(canonicalName, cancellationToken);
        }

        if (!_rowLoaded)
        {
            return ValueTask.FromResult(ConditionAttributeValue.Deferred);
        }

        return ValueTask.FromResult(_row is null ? ConditionAttributeValue.Missing : _registration.ReadValue(_row, canonicalName));
    }
}
