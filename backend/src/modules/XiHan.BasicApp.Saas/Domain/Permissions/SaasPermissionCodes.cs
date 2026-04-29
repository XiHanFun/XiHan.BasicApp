#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasPermissionCodes
// Guid:8f28fe43-8942-4495-9d38-01557d2bbffd
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/29 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.Permissions;

/// <summary>
/// SaaS 模块权限码
/// </summary>
public static class SaasPermissionCodes
{
    /// <summary>
    /// 模块编码
    /// </summary>
    public const string Module = "saas";

    /// <summary>
    /// 全部权限码
    /// </summary>
    public static IReadOnlyCollection<string> All =>
    [
        Tenant.Read,
        Tenant.Create,
        Tenant.Update,
        Tenant.Status,
        TenantMember.Read,
        TenantEdition.Read,
        TenantEdition.Create,
        TenantEdition.Update,
        TenantEdition.Status,
        TenantEdition.Default,
        TenantEditionPermission.Read,
        TenantEditionPermission.Grant,
        TenantEditionPermission.Update,
        TenantEditionPermission.Revoke
    ];

    /// <summary>
    /// 租户权限码
    /// </summary>
    public static class Tenant
    {
        /// <summary>
        /// 查看租户
        /// </summary>
        public const string Read = "saas:tenant:read";

        /// <summary>
        /// 创建租户
        /// </summary>
        public const string Create = "saas:tenant:create";

        /// <summary>
        /// 更新租户
        /// </summary>
        public const string Update = "saas:tenant:update";

        /// <summary>
        /// 更新租户状态
        /// </summary>
        public const string Status = "saas:tenant:status";
    }

    /// <summary>
    /// 租户成员权限码
    /// </summary>
    public static class TenantMember
    {
        /// <summary>
        /// 查看租户成员
        /// </summary>
        public const string Read = "saas:tenant-member:read";
    }

    /// <summary>
    /// 租户版本权限码
    /// </summary>
    public static class TenantEdition
    {
        /// <summary>
        /// 查看租户版本
        /// </summary>
        public const string Read = "saas:tenant-edition:read";

        /// <summary>
        /// 创建租户版本
        /// </summary>
        public const string Create = "saas:tenant-edition:create";

        /// <summary>
        /// 更新租户版本
        /// </summary>
        public const string Update = "saas:tenant-edition:update";

        /// <summary>
        /// 更新租户版本状态
        /// </summary>
        public const string Status = "saas:tenant-edition:status";

        /// <summary>
        /// 设置默认租户版本
        /// </summary>
        public const string Default = "saas:tenant-edition:default";
    }

    /// <summary>
    /// 租户版本权限码
    /// </summary>
    public static class TenantEditionPermission
    {
        /// <summary>
        /// 查看租户版本权限
        /// </summary>
        public const string Read = "saas:tenant-edition-permission:read";

        /// <summary>
        /// 授予租户版本权限
        /// </summary>
        public const string Grant = "saas:tenant-edition-permission:grant";

        /// <summary>
        /// 更新租户版本权限
        /// </summary>
        public const string Update = "saas:tenant-edition-permission:update";

        /// <summary>
        /// 撤销租户版本权限
        /// </summary>
        public const string Revoke = "saas:tenant-edition-permission:revoke";
    }
}
