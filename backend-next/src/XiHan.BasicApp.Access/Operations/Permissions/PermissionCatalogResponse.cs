// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;

namespace XiHan.BasicApp.Access.Operations.Permissions;

/// <summary>
/// <c>permissions.list</c> 的响应：调用方作用侧可授予的权限目录。
/// </summary>
/// <param name="Items">权限，按权限码排序。</param>
public sealed record PermissionCatalogResponse(IReadOnlyList<PermissionResponse> Items);

/// <summary>
/// 一条权限描述。
/// </summary>
/// <param name="Code">权限码 <c>&lt;包&gt;.&lt;资源&gt;.&lt;动作&gt;</c>。</param>
/// <param name="DisplayName">显示名。</param>
/// <param name="Description">说明。</param>
/// <param name="Package">包。</param>
/// <param name="Resource">资源。</param>
/// <param name="Action">动作。</param>
/// <param name="Scope">作用侧。</param>
/// <param name="Audited">是否审计。</param>
public sealed record PermissionResponse(string Code, string DisplayName, string? Description, string Package, string Resource, string Action, PermissionScope Scope, bool Audited)
{
    /// <summary>
    /// 由描述投影。
    /// </summary>
    public static PermissionResponse From(PermissionDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return new PermissionResponse(descriptor.Code, descriptor.DisplayName, descriptor.Description, descriptor.Package, descriptor.Resource, descriptor.Action, descriptor.Scope, descriptor.Audited);
    }
}
