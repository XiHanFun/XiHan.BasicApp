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
    /// 转移租户所有权。
    /// </summary>
    public const string TenantsTransferOwner = "tenancy.tenants.transfer-owner";

    /// <summary>
    /// 查看租户成员：<c>tenant-members.list</c>。
    /// </summary>
    public const string TenantMembersRead = "tenancy.tenant-members.read";

    /// <summary>
    /// 加成员：<c>tenant-members.create</c>。
    /// </summary>
    public const string TenantMembersCreate = "tenancy.tenant-members.create";

    /// <summary>
    /// 移除成员：<c>tenant-members.delete</c>。
    /// </summary>
    public const string TenantMembersDelete = "tenancy.tenant-members.delete";

    /// <summary>
    /// 查看套餐：<c>editions.list / get</c>。
    /// </summary>
    public const string EditionsRead = "tenancy.editions.read";

    /// <summary>
    /// 新建套餐：<c>editions.create</c>。
    /// </summary>
    public const string EditionsCreate = "tenancy.editions.create";

    /// <summary>
    /// 修改套餐与白名单。
    /// </summary>
    /// <remarks>
    /// <c>editions.update / set-permissions</c>。
    /// </remarks>
    public const string EditionsUpdate = "tenancy.editions.update";

    /// <summary>
    /// 删除套餐：<c>editions.delete</c>。
    /// </summary>
    public const string EditionsDelete = "tenancy.editions.delete";

    /// <summary>
    /// 查看本租户的订阅（套餐、到期、席位与存储用量）。
    /// </summary>
    public const string SubscriptionRead = "tenancy.subscription.read";

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
        new(TenantsTransferOwner, "Transfer tenant ownership", PermissionScope.Platform, audited: true, description: "Hand a tenant's ownership to one of its users or members; the previous owner stays an administrator."),
        new(TenantMembersRead, "Read tenant members", PermissionScope.Platform, description: "View which users may enter a tenant as members."),
        new(TenantMembersCreate, "Add tenant members", PermissionScope.Platform, audited: true, description: "Let a user whose home tenant is elsewhere enter a tenant."),
        new(TenantMembersDelete, "Remove tenant members", PermissionScope.Platform, audited: true, description: "Revoke a member's access to a tenant."),
        new(EditionsRead, "Read editions", PermissionScope.Platform, description: "View editions and their permission whitelists."),
        new(EditionsCreate, "Create editions", PermissionScope.Platform, audited: true, description: "Create editions with an initial permission whitelist."),
        new(EditionsUpdate, "Update editions", PermissionScope.Platform, audited: true, description: "Edit editions and replace their permission whitelists; tenants on them follow immediately."),
        new(EditionsDelete, "Delete editions", PermissionScope.Platform, audited: true, description: "Delete editions that no tenant uses."),
        new(SubscriptionRead, "Read subscription", PermissionScope.Tenant, description: "View the tenant's own edition, expiry, seat and storage usage."),
    ];
}
