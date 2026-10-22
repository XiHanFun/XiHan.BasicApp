// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Options;

namespace XiHan.BasicApp.Saas.Application.Conditions;

/// <summary>
/// 条件资源登记选项：各模块经 <c>AddPermissionResource</c> 往这里加
/// </summary>
public sealed class PermissionResourceOptions
{
    /// <summary>
    /// 登记项
    /// </summary>
    public List<PermissionResourceRegistration> Registrations { get; } = [];
}

/// <summary>
/// 条件资源登记表：按权限码查登记的实体
/// </summary>
public interface IPermissionResourceRegistry
{
    /// <summary>
    /// 按权限码找登记项
    /// </summary>
    bool TryGet(string permissionCode, out PermissionResourceRegistration registration);
}

/// <summary>
/// 条件资源登记表
/// </summary>
public sealed class PermissionResourceRegistry : IPermissionResourceRegistry
{
    private readonly Dictionary<string, PermissionResourceRegistration> _registrations = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 构造函数：同一权限码登记两次即抛出
    /// </summary>
    public PermissionResourceRegistry(IOptions<PermissionResourceOptions> options)
    {
        foreach (var registration in options.Value.Registrations)
        {
            if (!_registrations.TryAdd(registration.PermissionCode, registration))
            {
                throw new InvalidOperationException($"权限码 '{registration.PermissionCode}' 的条件资源重复登记。");
            }
        }
    }

    /// <inheritdoc />
    public bool TryGet(string permissionCode, out PermissionResourceRegistration registration)
    {
        registration = null!;
        return !string.IsNullOrWhiteSpace(permissionCode)
            && _registrations.TryGetValue(permissionCode.Trim(), out registration!);
    }
}
