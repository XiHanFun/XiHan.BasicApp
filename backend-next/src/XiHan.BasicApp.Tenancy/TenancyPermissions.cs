// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;

namespace XiHan.BasicApp.Tenancy;

/// <summary>
/// 租户包的权限码：全部只在平台侧（租户主体不能被授予）。
/// </summary>
public static class TenancyPermissions
{
    /// <summary>
    /// 查看租户。
    /// </summary>
    public const string TenantsRead = "tenancy.tenants.read";

    /// <summary>
    /// 开通租户。
    /// </summary>
    public const string TenantsCreate = "tenancy.tenants.create";

    /// <summary>
    /// 修改租户资料。
    /// </summary>
    public const string TenantsUpdate = "tenancy.tenants.update";

    /// <summary>
    /// 停用 / 恢复租户。
    /// </summary>
    public const string TenantsSuspend = "tenancy.tenants.suspend";

    /// <summary>
    /// 删除租户。
    /// </summary>
    public const string TenantsDelete = "tenancy.tenants.delete";

    /// <summary>
    /// 初始化租户管理员。
    /// </summary>
    public const string TenantsInitializeAdmin = "tenancy.tenants.initialize-admin";

    /// <summary>
    /// 全部权限描述。
    /// </summary>
    public static IReadOnlyList<PermissionDescriptor> All { get; } =
    [
        new(TenantsRead, "Read tenants", PermissionScope.Platform, description: "View tenants and their administrators."),
        new(TenantsCreate, "Create tenants", PermissionScope.Platform, audited: true, description: "Provision a tenant and register it in the tenant directory."),
        new(TenantsUpdate, "Update tenants", PermissionScope.Platform, audited: true, description: "Edit tenant names, descriptions, expiry and order."),
        new(TenantsSuspend, "Suspend tenants", PermissionScope.Platform, audited: true, description: "Suspend and resume tenants; suspended tenants cannot sign in."),
        new(TenantsDelete, "Delete tenants", PermissionScope.Platform, audited: true, description: "Delete tenants without users and remove them from the directory."),
        new(TenantsInitializeAdmin, "Initialize tenant administrators", PermissionScope.Platform, audited: true, description: "Create the first administrator of a tenant with every tenant-side permission."),
    ];
}
