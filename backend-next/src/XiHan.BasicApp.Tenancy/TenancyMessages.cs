// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Localization.Abstractions.Messages;

namespace XiHan.BasicApp.Tenancy;

/// <summary>
/// 租户包的消息键；各区域文案在 <c>Localization/&lt;区域&gt;.json</c>。操作说明用约定键 <c>&lt;操作 ID&gt;.description</c>（<c>-</c> 换成 <c>_</c>）。
/// </summary>
public static class TenancyMessages
{
    /// <summary>
    /// 消息目录名。
    /// </summary>
    public const string CatalogName = "basicapp.tenancy";

    /// <summary>
    /// 操作 <c>tenants.create</c> 的说明。
    /// </summary>
    public const string CreateTenantDescription = "tenants.create.description";

    /// <summary>
    /// 操作 <c>tenants.get</c> 的说明。
    /// </summary>
    public const string GetTenantDescription = "tenants.get.description";

    /// <summary>
    /// 操作 <c>tenants.list</c> 的说明。
    /// </summary>
    public const string ListTenantsDescription = "tenants.list.description";

    /// <summary>
    /// 操作 <c>tenants.update</c> 的说明。
    /// </summary>
    public const string UpdateTenantDescription = "tenants.update.description";

    /// <summary>
    /// 操作 <c>tenants.suspend</c> 的说明。
    /// </summary>
    public const string SuspendTenantsDescription = "tenants.suspend.description";

    /// <summary>
    /// 操作 <c>tenants.resume</c> 的说明。
    /// </summary>
    public const string ResumeTenantsDescription = "tenants.resume.description";

    /// <summary>
    /// 操作 <c>tenants.delete</c> 的说明。
    /// </summary>
    public const string DeleteTenantsDescription = "tenants.delete.description";

    /// <summary>
    /// 操作 <c>tenants.initialize-admin</c> 的说明。
    /// </summary>
    public const string InitializeAdminDescription = "tenants.initialize_admin.description";

    /// <summary>
    /// 租户不存在；参数 <c>id</c>。
    /// </summary>
    public const string TenantNotFound = "tenancy.tenant.not_found";

    /// <summary>
    /// 免费套餐不能标价。
    /// </summary>
    public const string FreeEditionPriced = "tenancy.edition.free_priced";

    /// <summary>
    /// 租户存储空间不足（已用、上限与这次需要，MB）。
    /// </summary>
    public const string StorageQuotaExceeded = "tenancy.quota.storage";

    /// <summary>
    /// 租户席位已达上限（已用与上限）。
    /// </summary>
    public const string SeatQuotaExceeded = "tenancy.quota.seats";

    /// <summary>
    /// 订阅属于租户，平台没有订阅。
    /// </summary>
    public const string SubscriptionTenantOnly = "tenancy.subscription.tenant_only";

    /// <summary>
    /// 操作 <c>tenants.quotas</c> 的说明。
    /// </summary>
    public const string ListTenantQuotasDescription = "tenants.quotas.description";

    /// <summary>
    /// 操作 <c>subscription.get</c> 的说明。
    /// </summary>
    public const string GetSubscriptionDescription = "subscription.get.description";

    /// <summary>
    /// 租户 ID 已被占用；参数 <c>tenantId</c>。
    /// </summary>
    public const string TenantIdTaken = "tenancy.tenant.id_taken";

    /// <summary>
    /// 管理员已初始化；参数 <c>tenantId</c>。
    /// </summary>
    public const string AdminInitialized = "tenancy.tenant.admin_initialized";

    /// <summary>
    /// 租户下还有用户；参数 <c>tenantId</c>、<c>count</c>。
    /// </summary>
    public const string HasUsers = "tenancy.tenant.has_users";

    /// <summary>
    /// 只有平台主体能管理租户。
    /// </summary>
    public const string PlatformOnly = "tenancy.tenant.platform_only";

    /// <summary>
    /// 列表筛选不受支持；参数 <c>field</c>、<c>operator</c>。
    /// </summary>
    public const string FilterUnsupported = "tenancy.tenant.filter_unsupported";

    /// <summary>
    /// 列表排序字段不受支持；参数 <c>field</c>。
    /// </summary>
    public const string SortUnsupported = "tenancy.tenant.sort_unsupported";

    /// <summary>
    /// 管理员登录名已被占用；参数 <c>userName</c>。
    /// </summary>
    public const string AdminNameTaken = "tenancy.admin.name_taken";

    /// <summary>
    /// 没有租户初始化扩展。
    /// </summary>
    public const string NoInitializer = "tenancy.tenant.no_initializer";

    /// <summary>
    /// 操作 <c>tenants.mine</c> 的说明。
    /// </summary>
    public const string MyTenantsDescription = "tenants.mine.description";

    /// <summary>
    /// 操作 <c>tenant-members.list</c> 的说明（约定键把 <c>-</c> 换成 <c>_</c>）。
    /// </summary>
    public const string ListTenantMembersDescription = "tenant_members.list.description";

    /// <summary>
    /// 操作 <c>tenant-members.create</c> 的说明。
    /// </summary>
    public const string AddTenantMemberDescription = "tenant_members.create.description";

    /// <summary>
    /// 操作 <c>tenant-members.delete</c> 的说明。
    /// </summary>
    public const string RemoveTenantMembersDescription = "tenant_members.delete.description";

    /// <summary>
    /// 用户已是该租户的成员；参数 <c>tenantId</c>、<c>userId</c>。
    /// </summary>
    public const string MemberExists = "tenancy.member.exists";

    /// <summary>
    /// 成员行不存在；参数 <c>id</c>。
    /// </summary>
    public const string MemberNotFound = "tenancy.member.not_found";

    /// <summary>
    /// 要加为成员的用户不存在；参数 <c>userId</c>。
    /// </summary>
    public const string MemberUserNotFound = "tenancy.member.user_not_found";

    /// <summary>
    /// 操作 <c>editions.list</c> 的说明。
    /// </summary>
    public const string ListEditionsDescription = "editions.list.description";

    /// <summary>
    /// 操作 <c>editions.get</c> 的说明。
    /// </summary>
    public const string GetEditionDescription = "editions.get.description";

    /// <summary>
    /// 操作 <c>editions.create</c> 的说明。
    /// </summary>
    public const string CreateEditionDescription = "editions.create.description";

    /// <summary>
    /// 操作 <c>editions.update</c> 的说明。
    /// </summary>
    public const string UpdateEditionDescription = "editions.update.description";

    /// <summary>
    /// 操作 <c>editions.set-permissions</c> 的说明（约定键把 <c>-</c> 换成 <c>_</c>）。
    /// </summary>
    public const string SetEditionPermissionsDescription = "editions.set_permissions.description";

    /// <summary>
    /// 操作 <c>editions.delete</c> 的说明。
    /// </summary>
    public const string DeleteEditionsDescription = "editions.delete.description";

    /// <summary>
    /// 操作 <c>tenants.change-edition</c> 的说明。
    /// </summary>
    public const string ChangeTenantEditionDescription = "tenants.change_edition.description";

    /// <summary>
    /// 套餐不存在；参数 <c>id</c>。
    /// </summary>
    public const string EditionNotFound = "tenancy.edition.not_found";

    /// <summary>
    /// 套餐编码已被占用；参数 <c>code</c>。
    /// </summary>
    public const string EditionCodeTaken = "tenancy.edition.code_taken";

    /// <summary>
    /// 套餐还有租户在用；参数 <c>code</c>。
    /// </summary>
    public const string EditionInUse = "tenancy.edition.in_use";

    /// <summary>
    /// 白名单里的权限不是租户能生效的；参数 <c>code</c>。
    /// </summary>
    public const string EditionPermissionInvalid = "tenancy.edition.permission_invalid";

    /// <summary>
    /// 没有缺省套餐。
    /// </summary>
    public const string NoDefaultEdition = "tenancy.edition.no_default";

    private static readonly Lazy<MessageCatalog> _catalog = new(static () => MessageCatalog.FromEmbeddedResources(CatalogName, typeof(TenancyMessages)));

    /// <summary>
    /// 本包的消息目录（嵌入的 10 种区域文件）。
    /// </summary>
    public static MessageCatalog Catalog => _catalog.Value;
}
